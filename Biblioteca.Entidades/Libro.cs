namespace Biblioteca.Entidades
{
    public class Libro
    {
        public int LibroId { get; set; }
        public string Titulo { get; set; }
        public string ISBN { get; set; }
        public int AutorId { get; set; }
        public int Ejemplares { get; set; }
        public bool Activo { get; set; }

        // Auxiliares para mostrar en pantalla
        public string NombreAutor { get; set; }
        public string Estado { get { return Activo ? "Activo" : "Inactivo"; } }
    }
}
