using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Biblioteca.Entidades;

namespace Biblioteca.Datos
{
    public class LibroDatos
    {
        private const string SelectBase =
            "SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, a.Nombre AS NombreAutor, l.Ejemplares, l.Activo " +
            "FROM Libros l INNER JOIN Autores a ON a.AutorId = l.AutorId ";

        /// <summary>Lista libros (activos e inactivos) cuyo título o autor contenga el filtro.</summary>
        public Task<List<Libro>> ListarAsync(string filtro)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectBase +
                    "WHERE (@Filtro = N'' OR l.Titulo LIKE @Patron OR a.Nombre LIKE @Patron) ORDER BY l.Titulo";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    filtro = (filtro ?? string.Empty).Trim();
                    cmd.Parameters.Add("@Filtro", SqlDbType.NVarChar, 200).Value = filtro;
                    cmd.Parameters.Add("@Patron", SqlDbType.NVarChar, 202).Value = "%" + filtro + "%";
                    return await LeerLibrosAsync(cmd).ConfigureAwait(false);
                }
            });
        }

        public Task<List<Libro>> ListarActivosAsync()
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectBase + "WHERE l.Activo = 1 ORDER BY l.Titulo";
                using (var cmd = new SqlCommand(sql, cn))
                    return await LeerLibrosAsync(cmd).ConfigureAwait(false);
            });
        }

        public Task<Libro> ObtenerAsync(int libroId)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectBase + "WHERE l.LibroId = @LibroId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                    var lista = await LeerLibrosAsync(cmd).ConfigureAwait(false);
                    return lista.Count > 0 ? lista[0] : null;
                }
            });
        }

        public Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = "SELECT COUNT(1) FROM Libros WHERE ISBN = @ISBN AND LibroId <> @LibroId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = isbn;
                    cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = excluirLibroId;
                    return (int)await cmd.ExecuteScalarAsync().ConfigureAwait(false) > 0;
                }
            });
        }

        public Task<int> InsertarAsync(Libro libro)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql =
                    "INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares, Activo) " +
                    "OUTPUT INSERTED.LibroId VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares, 1)";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    AgregarParametros(cmd, libro);
                    return (int)await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                }
            });
        }

        public Task ActualizarAsync(Libro libro)
        {
            return Conexion.EjecutarSinResultadoAsync(async cn =>
            {
                const string sql =
                    "UPDATE Libros SET Titulo = @Titulo, ISBN = @ISBN, AutorId = @AutorId, Ejemplares = @Ejemplares " +
                    "WHERE LibroId = @LibroId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    AgregarParametros(cmd, libro);
                    cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libro.LibroId;
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            });
        }

        /// <summary>Eliminación lógica: Activo = 0.</summary>
        public Task DarDeBajaAsync(int libroId)
        {
            return Conexion.EjecutarSinResultadoAsync(async cn =>
            {
                const string sql = "UPDATE Libros SET Activo = 0 WHERE LibroId = @LibroId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            });
        }

        public Task<bool> TienePrestamosPendientesAsync(int libroId)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = "SELECT COUNT(1) FROM DetallePrestamo WHERE LibroId = @LibroId AND FechaDevolucion IS NULL";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                    return (int)await cmd.ExecuteScalarAsync().ConfigureAwait(false) > 0;
                }
            });
        }

        private static void AgregarParametros(SqlCommand cmd, Libro libro)
        {
            cmd.Parameters.Add("@Titulo", SqlDbType.NVarChar, 200).Value = libro.Titulo;
            cmd.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = libro.ISBN;
            cmd.Parameters.Add("@AutorId", SqlDbType.Int).Value = libro.AutorId;
            cmd.Parameters.Add("@Ejemplares", SqlDbType.Int).Value = libro.Ejemplares;
        }

        private static async Task<List<Libro>> LeerLibrosAsync(SqlCommand cmd)
        {
            var lista = new List<Libro>();
            using (var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await r.ReadAsync().ConfigureAwait(false))
                {
                    lista.Add(new Libro
                    {
                        LibroId = (int)r["LibroId"],
                        Titulo = (string)r["Titulo"],
                        ISBN = (string)r["ISBN"],
                        AutorId = (int)r["AutorId"],
                        NombreAutor = (string)r["NombreAutor"],
                        Ejemplares = (int)r["Ejemplares"],
                        Activo = (bool)r["Activo"]
                    });
                }
            }
            return lista;
        }
    }
}
