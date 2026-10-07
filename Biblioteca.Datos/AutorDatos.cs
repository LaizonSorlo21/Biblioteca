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
                const string sql = "SELECT AutorId, Nombre, Nacionalidad, Activo FROM Autores WHERE Activo = 1 ORDER BY Nombre";
                using (var cmd = new SqlCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await r.ReadAsync().ConfigureAwait(false))
                    {
                        lista.Add(new Autor
                        {
                            AutorId = (int)r["AutorId"],
                            Nombre = (string)r["Nombre"],
                            Nacionalidad = r["Nacionalidad"] as string,
                            Activo = (bool)r["Activo"]
                        });
                    }
                }
                return lista;
            });
        }
    }
}
