using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio
{
    public class LibroNegocio
    {
        private readonly LibroDatos _libros;
        private readonly AutorDatos _autores;

        public LibroNegocio() : this(new LibroDatos(), new AutorDatos()) { }

        public LibroNegocio(LibroDatos libros, AutorDatos autores)
        {
            _libros = libros;
            _autores = autores;
        }

        public Task<List<Libro>> ListarAsync(string filtro) => _libros.ListarAsync(filtro);
        public Task<List<Libro>> ListarActivosAsync() => _libros.ListarActivosAsync();
        public Task<List<Autor>> ListarAutoresAsync() => _autores.ListarActivosAsync();

        public async Task<int> InsertarAsync(Libro libro)
        {
            await ValidarAsync(libro, 0).ConfigureAwait(false);
            libro.Activo = true;
            return await _libros.InsertarAsync(libro).ConfigureAwait(false);
        }

        public async Task ActualizarAsync(Libro libro)
        {
            if (libro == null || libro.LibroId <= 0)
                throw new ReglaNegocioException("Seleccione el libro que desea actualizar.");

            Libro existente = await _libros.ObtenerAsync(libro.LibroId).ConfigureAwait(false);
            if (existente == null)
                throw new ReglaNegocioException("El libro seleccionado ya no existe.");
            if (!existente.Activo)
                throw new ReglaNegocioException("No se puede actualizar un libro dado de baja.");

            await ValidarAsync(libro, libro.LibroId).ConfigureAwait(false);
            await _libros.ActualizarAsync(libro).ConfigureAwait(false);
        }

        public async Task DarDeBajaAsync(int libroId)
        {
            Libro libro = await _libros.ObtenerAsync(libroId).ConfigureAwait(false);
            if (libro == null)
                throw new ReglaNegocioException("El libro seleccionado no existe.");
            if (!libro.Activo)
                throw new ReglaNegocioException("El libro ya está dado de baja.");
            if (await _libros.TienePrestamosPendientesAsync(libroId).ConfigureAwait(false))
                throw new ReglaNegocioException("No se puede dar de baja el libro porque tiene préstamos pendientes.");

            await _libros.DarDeBajaAsync(libroId).ConfigureAwait(false);
        }

        private async Task ValidarAsync(Libro libro, int excluirLibroId)
        {
            if (libro == null)
                throw new ReglaNegocioException("Los datos del libro son obligatorios.");

            libro.Titulo = (libro.Titulo ?? string.Empty).Trim();
            libro.ISBN = (libro.ISBN ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(libro.Titulo))
                throw new ReglaNegocioException("Ingrese el título del libro.");
            if (string.IsNullOrEmpty(libro.ISBN))
                throw new ReglaNegocioException("Ingrese el ISBN del libro.");
            if (libro.Ejemplares < 0)
                throw new ReglaNegocioException("La cantidad de ejemplares no puede ser negativa.");

            if (libro.AutorId <= 0 || !await _autores.ExisteActivoAsync(libro.AutorId).ConfigureAwait(false))
                throw new ReglaNegocioException("Seleccione un autor válido.");

            if (await _libros.ExisteIsbnAsync(libro.ISBN, excluirLibroId).ConfigureAwait(false))
                throw new ReglaNegocioException($"Ya existe un libro con el ISBN {libro.ISBN}.");
        }
    }
}