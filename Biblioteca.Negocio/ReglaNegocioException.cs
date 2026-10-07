using System;

namespace Biblioteca.Negocio
{
    /// <summary>Se lanza cuando una operación incumple una regla de negocio. El mensaje es apto para el usuario.</summary>
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje) : base(mensaje) { }
    }
}
