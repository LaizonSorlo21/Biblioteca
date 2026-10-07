using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Biblioteca.Entidades;

namespace Biblioteca.Datos
{
    public class SocioDatos
    {
        private const string SelectBase = "SELECT SocioId, DNI, Nombre, Email, Activo FROM Socios ";

        /// <summary>Lista socios (activos e inactivos) cuyo nombre o DNI contenga el filtro.</summary>
        public Task<List<Socio>> ListarAsync(string filtro)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectBase +
                    "WHERE (@Filtro = N'' OR Nombre LIKE @Patron OR DNI LIKE @Patron) ORDER BY Nombre";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    filtro = (filtro ?? string.Empty).Trim();
                    cmd.Parameters.Add("@Filtro", SqlDbType.NVarChar, 100).Value = filtro;
                    cmd.Parameters.Add("@Patron", SqlDbType.NVarChar, 102).Value = "%" + filtro + "%";
                    return await LeerSociosAsync(cmd).ConfigureAwait(false);
                }
            });
        }

        public Task<List<Socio>> ListarActivosAsync()
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectBase + "WHERE Activo = 1 ORDER BY Nombre";
                using (var cmd = new SqlCommand(sql, cn))
                    return await LeerSociosAsync(cmd).ConfigureAwait(false);
            });
        }

        public Task<Socio> ObtenerAsync(int socioId)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectBase + "WHERE SocioId = @SocioId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socioId;
                    var lista = await LeerSociosAsync(cmd).ConfigureAwait(false);
                    return lista.Count > 0 ? lista[0] : null;
                }
            });
        }

        public Task<bool> ExisteDniAsync(string dni, int excluirSocioId)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = "SELECT COUNT(1) FROM Socios WHERE DNI = @DNI AND SocioId <> @SocioId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@DNI", SqlDbType.NVarChar, 8).Value = dni;
                    cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = excluirSocioId;
                    return (int)await cmd.ExecuteScalarAsync().ConfigureAwait(false) > 0;
                }
            });
        }

        public Task<int> InsertarAsync(Socio socio)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql =
                    "INSERT INTO Socios (DNI, Nombre, Email, Activo) OUTPUT INSERTED.SocioId " +
                    "VALUES (@DNI, @Nombre, @Email, 1)";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    AgregarParametros(cmd, socio);
                    return (int)await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                }
            });
        }

        public Task ActualizarAsync(Socio socio)
        {
            return Conexion.EjecutarSinResultadoAsync(async cn =>
            {
                const string sql = "UPDATE Socios SET DNI = @DNI, Nombre = @Nombre, Email = @Email WHERE SocioId = @SocioId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    AgregarParametros(cmd, socio);
                    cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socio.SocioId;
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            });
        }

        /// <summary>Eliminación lógica: Activo = 0.</summary>
        public Task DarDeBajaAsync(int socioId)
        {
            return Conexion.EjecutarSinResultadoAsync(async cn =>
            {
                const string sql = "UPDATE Socios SET Activo = 0 WHERE SocioId = @SocioId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socioId;
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            });
        }

        /// <summary>Cantidad de libros que el socio aún no ha devuelto.</summary>
        public Task<int> ContarLibrosPendientesAsync(int socioId)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql =
                    "SELECT COUNT(1) FROM DetallePrestamo d INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId " +
                    "WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socioId;
                    return (int)await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                }
            });
        }

        private static void AgregarParametros(SqlCommand cmd, Socio socio)
        {
            cmd.Parameters.Add("@DNI", SqlDbType.NVarChar, 8).Value = socio.DNI;
            cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = socio.Nombre;
            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = (object)socio.Email ?? System.DBNull.Value;
        }

        private static async Task<List<Socio>> LeerSociosAsync(SqlCommand cmd)
        {
            var lista = new List<Socio>();
            using (var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await r.ReadAsync().ConfigureAwait(false))
                {
                    lista.Add(new Socio
                    {
                        SocioId = (int)r["SocioId"],
                        DNI = (string)r["DNI"],
                        Nombre = (string)r["Nombre"],
                        Email = r["Email"] as string,
                        Activo = (bool)r["Activo"]
                    });
                }
            }
            return lista;
        }
    }
}
