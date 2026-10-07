using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Biblioteca.Datos
{
    /// <summary>Error técnico de acceso a datos, con mensaje apto para el usuario.</summary>
    public class AccesoDatosException : Exception
    {
        public AccesoDatosException(string mensaje) : base(mensaje) { }
        public AccesoDatosException(string mensaje, Exception inner) : base(mensaje, inner) { }
    }

    /// <summary>Operación rechazada por el estado actual de los datos (p. ej. sin stock por concurrencia).</summary>
    public class OperacionRechazadaException : Exception
    {
        public OperacionRechazadaException(string mensaje) : base(mensaje) { }
    }

    /// <summary>Crea conexiones usando la cadena "BibliotecaDB" definida en el App.config de la aplicación.</summary>
    public static class Conexion
    {
        private const string NombreCadena = "BibliotecaDB";

        private static string ObtenerCadena()
        {
            ConnectionStringSettings cs = ConfigurationManager.ConnectionStrings[NombreCadena];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                throw new AccesoDatosException("No se encontró la cadena de conexión '" + NombreCadena + "' en el App.config.");
            return cs.ConnectionString;
        }

        /// <summary>Abre una conexión, ejecuta la acción y convierte SqlException en AccesoDatosException.</summary>
        public static async Task<T> EjecutarAsync<T>(Func<SqlConnection, Task<T>> accion)
        {
            try
            {
                using (var cn = new SqlConnection(ObtenerCadena()))
                {
                    await cn.OpenAsync().ConfigureAwait(false);
                    return await accion(cn).ConfigureAwait(false);
                }
            }
            catch (SqlException ex)
            {
                throw new AccesoDatosException(Traducir(ex), ex);
            }
        }

        public static Task EjecutarSinResultadoAsync(Func<SqlConnection, Task> accion)
        {
            return EjecutarAsync<bool>(async cn =>
            {
                await accion(cn).ConfigureAwait(false);
                return true;
            });
        }

        private static string Traducir(SqlException ex)
        {
            switch (ex.Number)
            {
                case 2627:
                case 2601:
                    return "Ya existe un registro con el mismo valor único (ISBN o DNI).";
                case 547:
                    return "La operación no es válida porque el registro está relacionado con otros datos.";
                default:
                    return "No se pudo acceder a la base de datos BibliotecaDB. Verifique que SQL Server esté activo y que la cadena de conexión sea correcta.";
            }
        }
    }
}
