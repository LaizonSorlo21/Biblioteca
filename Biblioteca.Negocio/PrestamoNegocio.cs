using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio
{
    public class ResultadoDevolucion
    {
        public int DiasRetraso { get; set; }
        public decimal Multa { get; set; }
    }

    public class PrestamoNegocio
    {
        public const int MaxLibrosPendientes = 3;
        public const decimal MultaPorDia = 1.50m;
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoDevuelto = "Devuelto";

        private readonly SocioDatos _socios;
        private readonly LibroDatos _libros;
        private readonly PrestamoDatos _prestamos;

        public PrestamoNegocio() : this(new SocioDatos(), new LibroDatos(), new PrestamoDatos()) { }

        public PrestamoNegocio(SocioDatos socios, LibroDatos libros, PrestamoDatos prestamos)
        {
            _socios = socios;
            _libros = libros;
            _prestamos = prestamos;
        }

        /// <summary>Valida todas las reglas y registra el préstamo (cabecera, detalle y stock en una transacción).</summary>
        public async Task<int> RegistrarAsync(int socioId, IEnumerable<int> libroIds, DateTime fechaLimite)
        {
            List<int> ids = (libroIds ?? Enumerable.Empty<int>()).ToList();
            DateTime hoy = DateTime.Today;

            if (socioId <= 0)
                throw new ReglaNegocioException("Seleccione un socio.");
            if (ids.Count == 0)
                throw new ReglaNegocioException("Agregue al menos un libro al préstamo.");
            if (ids.Distinct().Count() != ids.Count)
                throw new ReglaNegocioException("No se puede repetir un libro dentro del mismo préstamo.");
            if (fechaLimite.Date < hoy)
                throw new ReglaNegocioException("La fecha límite no puede ser anterior a la fecha del préstamo.");

            Socio socio = await _socios.ObtenerAsync(socioId).ConfigureAwait(false);
            if (socio == null)
                throw new ReglaNegocioException("El socio seleccionado no existe.");
            if (!socio.Activo)
                throw new ReglaNegocioException("El socio " + socio.Nombre + " está dado de baja y no puede solicitar préstamos.");

            int pendientes = await _socios.ContarLibrosPendientesAsync(socioId).ConfigureAwait(false);
            int disponibles = MaxLibrosPendientes - pendientes;
            if (ids.Count > disponibles)
            {
                if (disponibles <= 0)
                    throw new ReglaNegocioException("El socio ya tiene " + pendientes + " libros pendientes (máximo " + MaxLibrosPendientes + "). Debe devolver alguno antes de pedir otro.");
                throw new ReglaNegocioException("El socio tiene " + pendientes + " libro(s) pendiente(s) y solo puede prestar " + disponibles + " más (máximo " + MaxLibrosPendientes + ").");
            }

            var prestamo = new Prestamo
            {
                SocioId = socioId,
                FechaPrestamo = hoy,
                FechaLimite = fechaLimite.Date,
                Estado = EstadoPendiente
            };

            foreach (int libroId in ids)
            {
                Libro libro = await _libros.ObtenerAsync(libroId).ConfigureAwait(false);
                if (libro == null)
                    throw new ReglaNegocioException("Uno de los libros seleccionados no existe.");
                if (!libro.Activo)
                    throw new ReglaNegocioException("El libro \"" + libro.Titulo + "\" está dado de baja.");
                if (libro.Ejemplares <= 0)
                    throw new ReglaNegocioException("El libro \"" + libro.Titulo + "\" no tiene ejemplares disponibles.");

                prestamo.Detalles.Add(new DetallePrestamo { LibroId = libroId });
            }

            try
            {
                return await _prestamos.RegistrarAsync(prestamo).ConfigureAwait(false);
            }
            catch (OperacionRechazadaException ex)
            {
                throw new ReglaNegocioException(ex.Message);
            }
        }

        /// <summary>Devuelve un libro: calcula la multa, repone el ejemplar y cierra el préstamo si ya no quedan pendientes.</summary>
        public async Task<ResultadoDevolucion> DevolverAsync(int prestamoId, int libroId)
        {
            DetallePrestamo detalle = await _prestamos.ObtenerDetalleAsync(prestamoId, libroId).ConfigureAwait(false);
            if (detalle == null)
                throw new ReglaNegocioException("El libro no pertenece al préstamo indicado.");
            if (detalle.FechaDevolucion.HasValue)
                throw new ReglaNegocioException("Este libro ya fue devuelto.");

            DateTime fechaDevolucion = DateTime.Today;
            var resultado = new ResultadoDevolucion
            {
                DiasRetraso = CalcularDiasRetraso(detalle.FechaLimite, fechaDevolucion),
                Multa = CalcularMulta(detalle.FechaLimite, fechaDevolucion)
            };

            try
            {
                await _prestamos.RegistrarDevolucionAsync(prestamoId, libroId, fechaDevolucion, EstadoDevuelto).ConfigureAwait(false);
            }
            catch (OperacionRechazadaException ex)
            {
                throw new ReglaNegocioException(ex.Message);
            }

            return resultado;
        }

        public static int CalcularDiasRetraso(DateTime fechaLimite, DateTime fechaDevolucion)
        {
            int dias = (fechaDevolucion.Date - fechaLimite.Date).Days;
            return dias > 0 ? dias : 0;
        }

        /// <summary>Multa = días de retraso x S/ 1.50.</summary>
        public static decimal CalcularMulta(DateTime fechaLimite, DateTime fechaDevolucion)
        {
            return CalcularDiasRetraso(fechaLimite, fechaDevolucion) * MultaPorDia;
        }

        public static string FormatearMulta(decimal multa)
        {
            return "S/ " + multa.ToString("F2", CultureInfo.InvariantCulture);
        }

        public Task<List<DetallePrestamo>> ListarPendientesAsync(string filtro)
        {
            return _prestamos.ListarPendientesAsync(filtro);
        }

        public async Task<List<DetallePrestamo>> ReportePorFechasAsync(DateTime desde, DateTime hasta)
        {
            if (desde.Date > hasta.Date)
                throw new ReglaNegocioException("La fecha \"desde\" no puede ser posterior a la fecha \"hasta\".");
            return await _prestamos.ListarPorFechasAsync(desde, hasta).ConfigureAwait(false);
        }
    }
}
