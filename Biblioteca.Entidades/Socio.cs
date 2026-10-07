namespace Biblioteca.Entidades
{
    public class Socio
    {
        public int SocioId { get; set; }
        public string DNI { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public bool Activo { get; set; }

        // Auxiliar para mostrar en pantalla
        public string Estado { get { return Activo ? "Activo" : "Inactivo"; } }
    }
}
