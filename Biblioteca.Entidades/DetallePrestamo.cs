using System;

namespace Biblioteca.Entidades
{
    public class DetallePrestamo
    {
        public int PrestamoId { get; set; }
        public int LibroId { get; set; }
        public DateTime? FechaDevolucion { get; set; }

        // Auxiliares para listados y reportes (provienen de JOIN)
        public string NombreSocio { get; set; }
        public string TituloLibro { get; set; }
        public DateTime FechaPrestamo { get; set; }
        public DateTime FechaLimite { get; set; }
        public string EstadoPrestamo { get; set; }
    }
}
