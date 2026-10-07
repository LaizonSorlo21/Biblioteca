using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Biblioteca.Entidades;

namespace Biblioteca.Datos
{
    public class AutorDatos
    {
        public Task<List<Autor>> ListarActivosAsync()
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                var lista = new List<Autor>();
                const string sql = @"SELECT AutorId, Nombre, Nacionalidad, Activo 
                                     FROM Autores 
                                     WHERE Activo = 1 
                                     ORDER BY Nombre";

                using (var cmd = new SqlCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    int idOrdinal = r.GetOrdinal("AutorId");
                    int nombreOrdinal = r.GetOrdinal("Nombre");
                    int nacOrdinal = r.GetOrdinal("Nacionalidad");
                    int activoOrdinal = r.GetOrdinal("Activo");

                    while (await r.ReadAsync().ConfigureAwait(false))
                    {
                        lista.Add(new Autor
                        {
                            AutorId = r.GetInt32(idOrdinal),
                            Nombre = r.GetString(nombreOrdinal),
                            Nacionalidad = r.IsDBNull(nacOrdinal) ? null : r.GetString(nacOrdinal),
                            Activo = r.GetBoolean(activoOrdinal)
                        });
                    }
                }

                return lista;
            });
        }

        public Task<bool> ExisteActivoAsync(int autorId)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = "SELECT COUNT(1) FROM Autores WHERE AutorId = @AutorId AND Activo = 1";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@AutorId", SqlDbType.Int).Value = autorId;
                    return (int)await cmd.ExecuteScalarAsync().ConfigureAwait(false) > 0;
                }
            });
        }
    }
}
