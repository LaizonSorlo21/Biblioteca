using System;
using System.Collections.Generic;
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
    }
}