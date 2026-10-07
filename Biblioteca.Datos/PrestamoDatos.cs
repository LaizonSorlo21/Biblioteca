using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Biblioteca.Entidades;

namespace Biblioteca.Datos
{
    public class PrestamoDatos
    {
        private const string SelectDetalle = @"
            SELECT d.PrestamoId, d.LibroId, d.FechaDevolucion, p.FechaPrestamo, p.FechaLimite, p.Estado, 
                   s.Nombre AS NombreSocio, l.Titulo AS TituloLibro 
            FROM Prestamos p 
            INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId 
            INNER JOIN Libros l ON l.LibroId = d.LibroId 
            INNER JOIN Socios s ON s.SocioId = p.SocioId ";

        public Task<int> RegistrarAsync(Prestamo prestamo)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        int prestamoId;
                        const string sqlCab = @"
                            INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) 
                            OUTPUT INSERTED.PrestamoId 
                            VALUES (@SocioId, @FechaPrestamo, @FechaLimite, @Estado)";

                        using (var cmd = new SqlCommand(sqlCab, cn, tx))
                        {
                            cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = prestamo.SocioId;
                            cmd.Parameters.Add("@FechaPrestamo", SqlDbType.Date).Value = prestamo.FechaPrestamo.Date;
                            cmd.Parameters.Add("@FechaLimite", SqlDbType.Date).Value = prestamo.FechaLimite.Date;
                            cmd.Parameters.Add("@Estado", SqlDbType.NVarChar, 20).Value = prestamo.Estado;

                            object result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                            prestamoId = Convert.ToInt32(result);
                        }

                        const string sqlDet = "INSERT INTO DetallePrestamo (PrestamoId, LibroId) VALUES (@PrestamoId, @LibroId)";
                        const string sqlStock = @"
                            UPDATE Libros 
                            SET Ejemplares = Ejemplares - 1 
                            WHERE LibroId = @LibroId AND Activo = 1 AND Ejemplares > 0";

                        using (var cmdDet = new SqlCommand(sqlDet, cn, tx))
                        using (var cmdStock = new SqlCommand(sqlStock, cn, tx))
                        {
                            var pPrestamoId = cmdDet.Parameters.Add("@PrestamoId", SqlDbType.Int);
                            var pLibroIdDet = cmdDet.Parameters.Add("@LibroId", SqlDbType.Int);
                            var pLibroIdStock = cmdStock.Parameters.Add("@LibroId", SqlDbType.Int);

                            pPrestamoId.Value = prestamoId;

                            foreach (DetallePrestamo detalle in prestamo.Detalles)
                            {
                                pLibroIdDet.Value = detalle.LibroId;
                                await cmdDet.ExecuteNonQueryAsync().ConfigureAwait(false);

                                pLibroIdStock.Value = detalle.LibroId;
                                int filas = await cmdStock.ExecuteNonQueryAsync().ConfigureAwait(false);
                                if (filas == 0)
                                {
                                    throw new OperacionRechazadaException(
                                        "Uno de los libros ya no tiene ejemplares disponibles. No se registró el préstamo.");
                                }
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

        public Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion, string estadoCompleto)
        {
            return Conexion.EjecutarSinResultadoAsync(async cn =>
            {
                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        const string sqlDet = @"
                            UPDATE DetallePrestamo 
                            SET FechaDevolucion = @Fecha 
                            WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId AND FechaDevolucion IS NULL";

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

                        const string sqlEstado = @"
                            UPDATE Prestamos 
                            SET Estado = @Estado 
                            WHERE PrestamoId = @PrestamoId 
                            AND NOT EXISTS (SELECT 1 FROM DetallePrestamo WHERE PrestamoId = @PrestamoId AND FechaDevolucion IS NULL)";

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
                string sql = SelectDetalle + "WHERE d.PrestamoId = @PrestamoId AND d.LibroId = @LibroId";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                    cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                    var lista = await LeerDetallesAsync(cmd).ConfigureAwait(false);
                    return lista.Count > 0 ? lista[0] : null;
                }
            });
        }

        public Task<List<DetallePrestamo>> ListarPendientesAsync(string filtro)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                string sql = SelectDetalle + @"
                    WHERE d.FechaDevolucion IS NULL 
                    AND (@Filtro = N'' OR s.Nombre LIKE @Patron OR s.DNI LIKE @Patron OR l.Titulo LIKE @Patron) 
                    ORDER BY p.FechaLimite, p.PrestamoId";

                using (var cmd = new SqlCommand(sql, cn))
                {
                    filtro = (filtro ?? string.Empty).Trim();
                    cmd.Parameters.Add("@Filtro", SqlDbType.NVarChar, 200).Value = filtro;
                    cmd.Parameters.Add("@Patron", SqlDbType.NVarChar, 202).Value = "%" + filtro + "%";
                    return await LeerDetallesAsync(cmd).ConfigureAwait(false);
                }
            });
        }

        public Task<List<DetallePrestamo>> ListarPorFechasAsync(DateTime desde, DateTime hasta)
        {
            return Conexion.EjecutarAsync(async cn =>
            {
                string sql = SelectDetalle + @"
                    WHERE p.FechaPrestamo >= @Desde AND p.FechaPrestamo <= @Hasta 
                    ORDER BY p.FechaPrestamo DESC, p.PrestamoId DESC, l.Titulo";

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
                int prestamoIdOrd = r.GetOrdinal("PrestamoId");
                int libroIdOrd = r.GetOrdinal("LibroId");
                int fechaDevOrd = r.GetOrdinal("FechaDevolucion");
                int fechaPrestamoOrd = r.GetOrdinal("FechaPrestamo");
                int fechaLimiteOrd = r.GetOrdinal("FechaLimite");
                int estadoOrd = r.GetOrdinal("Estado");
                int nombreSocioOrd = r.GetOrdinal("NombreSocio");
                int tituloLibroOrd = r.GetOrdinal("TituloLibro");

                while (await r.ReadAsync().ConfigureAwait(false))
                {
                    lista.Add(new DetallePrestamo
                    {
                        PrestamoId = r.GetInt32(prestamoIdOrd),
                        LibroId = r.GetInt32(libroIdOrd),
                        FechaDevolucion = r.IsDBNull(fechaDevOrd) ? (DateTime?)null : r.GetDateTime(fechaDevOrd),
                        FechaPrestamo = r.GetDateTime(fechaPrestamoOrd),
                        FechaLimite = r.GetDateTime(fechaLimiteOrd),
                        EstadoPrestamo = r.GetString(estadoOrd),
                        NombreSocio = r.GetString(nombreSocioOrd),
                        TituloLibro = r.GetString(tituloLibroOrd)
                    });
                }
            }
            return lista;
        }
    }
}