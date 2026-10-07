using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Biblioteca.Entidades;

namespace Biblioteca.Datos
{
    /// <summary>Acceso a datos de préstamos. Las reglas de negocio las valida Biblioteca.Negocio.</summary>
    public class PrestamoDatos
    {
        private const string SelectDetalle =
            "SELECT d.PrestamoId, d.LibroId, d.FechaDevolucion, p.FechaPrestamo, p.FechaLimite, p.Estado, " +
            "s.Nombre AS NombreSocio, l.Titulo AS TituloLibro " +
            "FROM Prestamos p " +
            "INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId " +
            "INNER JOIN Libros l ON l.LibroId = d.LibroId " +
            "INNER JOIN Socios s ON s.SocioId = p.SocioId ";

        /// <summary>
        /// Inserta cabecera, detalle y descuenta ejemplares en UNA sola transacción.
        /// Si algo falla se hace ROLLBACK y no queda nada guardado.
        /// </summary>
        public Task<int> RegistrarAsync(Prestamo prestamo)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        int prestamoId;
                        const string sqlCab =
                            "INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) OUTPUT INSERTED.PrestamoId " +
                            "VALUES (@SocioId, @FechaPrestamo, @FechaLimite, @Estado)";
                        using (var cmd = new SqlCommand(sqlCab, cn, tx))
                        {
                            cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = prestamo.SocioId;
                            cmd.Parameters.Add("@FechaPrestamo", SqlDbType.Date).Value = prestamo.FechaPrestamo.Date;
                            cmd.Parameters.Add("@FechaLimite", SqlDbType.Date).Value = prestamo.FechaLimite.Date;
                            cmd.Parameters.Add("@Estado", SqlDbType.NVarChar, 20).Value = prestamo.Estado;
                            prestamoId = (int)await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                        }

                        foreach (DetallePrestamo detalle in prestamo.Detalles)
                        {
                            const string sqlDet = "INSERT INTO DetallePrestamo (PrestamoId, LibroId) VALUES (@PrestamoId, @LibroId)";
                            using (var cmd = new SqlCommand(sqlDet, cn, tx))
                            {
                                cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                                cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = detalle.LibroId;
                                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                            }

                            // El WHERE evita stock negativo si otro usuario tomó el último ejemplar.
                            const string sqlStock =
                                "UPDATE Libros SET Ejemplares = Ejemplares - 1 " +
                                "WHERE LibroId = @LibroId AND Activo = 1 AND Ejemplares > 0";
                            using (var cmd = new SqlCommand(sqlStock, cn, tx))
                            {
                                cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = detalle.LibroId;
                                int filas = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                                if (filas == 0)
                                    throw new OperacionRechazadaException(
                                        "Uno de los libros ya no tiene ejemplares disponibles. No se registró el préstamo.");
                            }
                        }

                        tx.Commit();
                        return prestamoId;
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch (InvalidOperationException) { }
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// Marca el libro como devuelto, repone 1 ejemplar y, si no quedan libros pendientes en el préstamo,
        /// asigna <paramref name="estadoCompleto"/> a la cabecera. Todo en una transacción.
        /// </summary>
        public Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion, string estadoCompleto)
        {
            return Conexion.EjecutarSinResultadoAsync(async cn =>
            {
                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        const string sqlDet =
                            "UPDATE DetallePrestamo SET FechaDevolucion = @Fecha " +
                            "WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId AND FechaDevolucion IS NULL";
                        using (var cmd = new SqlCommand(sqlDet, cn, tx))
                        {
                            cmd.Parameters.Add("@Fecha", SqlDbType.Date).Value = fechaDevolucion.Date;
                            cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                            cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                            int filas = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                            if (filas == 0)
                                throw new OperacionRechazadaException("Este libro ya había sido devuelto.");
                        }

                        const string sqlStock = "UPDATE Libros SET Ejemplares = Ejemplares + 1 WHERE LibroId = @LibroId";
                        using (var cmd = new SqlCommand(sqlStock, cn, tx))
                        {
                            cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }

                        const string sqlEstado =
                            "UPDATE Prestamos SET Estado = @Estado WHERE PrestamoId = @PrestamoId " +
                            "AND NOT EXISTS (SELECT 1 FROM DetallePrestamo WHERE PrestamoId = @PrestamoId AND FechaDevolucion IS NULL)";
                        using (var cmd = new SqlCommand(sqlEstado, cn, tx))
                        {
                            cmd.Parameters.Add("@Estado", SqlDbType.NVarChar, 20).Value = estadoCompleto;
                            cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch (InvalidOperationException) { }
                        throw;
                    }
                }
            });
        }

        public Task<DetallePrestamo> ObtenerDetalleAsync(int prestamoId, int libroId)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectDetalle + "WHERE d.PrestamoId = @PrestamoId AND d.LibroId = @LibroId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                    cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                    var lista = await LeerDetallesAsync(cmd).ConfigureAwait(false);
                    return lista.Count > 0 ? lista[0] : null;
                }
            });
        }

        /// <summary>Libros aún no devueltos, filtrados por socio (nombre o DNI) o título.</summary>
        public Task<List<DetallePrestamo>> ListarPendientesAsync(string filtro)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectDetalle +
                    "WHERE d.FechaDevolucion IS NULL AND (@Filtro = N'' OR s.Nombre LIKE @Patron OR s.DNI LIKE @Patron OR l.Titulo LIKE @Patron) " +
                    "ORDER BY p.FechaLimite, p.PrestamoId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    filtro = (filtro ?? string.Empty).Trim();
                    cmd.Parameters.Add("@Filtro", SqlDbType.NVarChar, 200).Value = filtro;
                    cmd.Parameters.Add("@Patron", SqlDbType.NVarChar, 202).Value = "%" + filtro + "%";
                    return await LeerDetallesAsync(cmd).ConfigureAwait(false);
                }
            });
        }

        /// <summary>Reporte de préstamos por rango de fechas (INNER JOIN Prestamos-DetallePrestamo-Libros-Socios).</summary>
        public Task<List<DetallePrestamo>> ListarPorFechasAsync(DateTime desde, DateTime hasta)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                const string sql = SelectDetalle +
                    "WHERE p.FechaPrestamo >= @Desde AND p.FechaPrestamo <= @Hasta " +
                    "ORDER BY p.FechaPrestamo DESC, p.PrestamoId DESC, l.Titulo";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.Date;
                    cmd.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta.Date;
                    return await LeerDetallesAsync(cmd).ConfigureAwait(false);
                }
            });
        }

        private static async Task<List<DetallePrestamo>> LeerDetallesAsync(SqlCommand cmd)
        {
            var lista = new List<DetallePrestamo>();
            using (var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await r.ReadAsync().ConfigureAwait(false))
                {
                    lista.Add(new DetallePrestamo
                    {
                        PrestamoId = (int)r["PrestamoId"],
                        LibroId = (int)r["LibroId"],
                        FechaDevolucion = r["FechaDevolucion"] == DBNull.Value ? (DateTime?)null : (DateTime)r["FechaDevolucion"],
                        FechaPrestamo = (DateTime)r["FechaPrestamo"],
                        FechaLimite = (DateTime)r["FechaLimite"],
                        EstadoPrestamo = (string)r["Estado"],
                        NombreSocio = (string)r["NombreSocio"],
                        TituloLibro = (string)r["TituloLibro"]
                    });
                }
            }
            return lista;
        }
    }
}
