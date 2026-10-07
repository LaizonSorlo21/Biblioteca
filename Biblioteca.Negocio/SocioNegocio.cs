using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio
{
    public class SocioNegocio
    {
        private readonly SocioDatos _datos;

        public SocioNegocio() : this(new SocioDatos()) { }

        public SocioNegocio(SocioDatos datos)
        {
            _datos = datos;
        }

        public Task<List<Socio>> ListarAsync(string filtro) { return _datos.ListarAsync(filtro); }
        public Task<List<Socio>> ListarActivosAsync() { return _datos.ListarActivosAsync(); }

        public async Task<int> InsertarAsync(Socio socio)
        {
            await ValidarAsync(socio, 0).ConfigureAwait(false);
            socio.Activo = true;
            return await _datos.InsertarAsync(socio).ConfigureAwait(false);
        }

        public async Task ActualizarAsync(Socio socio)
        {
            if (socio == null || socio.SocioId <= 0)
                throw new ReglaNegocioException("Seleccione el socio que desea actualizar.");

            Socio existente = await _datos.ObtenerAsync(socio.SocioId).ConfigureAwait(false);
            if (existente == null)
                throw new ReglaNegocioException("El socio seleccionado ya no existe.");
            if (!existente.Activo)
                throw new ReglaNegocioException("No se puede actualizar un socio dado de baja.");

            await ValidarAsync(socio, socio.SocioId).ConfigureAwait(false);
            await _datos.ActualizarAsync(socio).ConfigureAwait(false);
        }

        /// <summary>Baja lógica (Activo = 0). Nunca elimina físicamente.</summary>
        public async Task DarDeBajaAsync(int socioId)
        {
            Socio socio = await _datos.ObtenerAsync(socioId).ConfigureAwait(false);
            if (socio == null)
                throw new ReglaNegocioException("El socio seleccionado no existe.");
            if (!socio.Activo)
                throw new ReglaNegocioException("El socio ya está dado de baja.");

            int pendientes = await _datos.ContarLibrosPendientesAsync(socioId).ConfigureAwait(false);
            if (pendientes > 0)
                throw new ReglaNegocioException("No se puede dar de baja al socio porque tiene " + pendientes + " libro(s) pendiente(s) de devolución.");

            await _datos.DarDeBajaAsync(socioId).ConfigureAwait(false);
        }

        private async Task ValidarAsync(Socio socio, int excluirSocioId)
        {
            if (socio == null)
                throw new ReglaNegocioException("Los datos del socio son obligatorios.");

            socio.DNI = (socio.DNI ?? string.Empty).Trim();
            socio.Nombre = (socio.Nombre ?? string.Empty).Trim();
            socio.Email = string.IsNullOrWhiteSpace(socio.Email) ? null : socio.Email.Trim();

            if (socio.DNI.Length != 8 || !socio.DNI.All(char.IsDigit))
                throw new ReglaNegocioException("El DNI debe tener exactamente 8 dígitos.");
            if (socio.Nombre.Length == 0)
                throw new ReglaNegocioException("Ingrese el nombre del socio.");
            if (socio.Email != null && !EmailValido(socio.Email))
                throw new ReglaNegocioException("El correo electrónico no tiene un formato válido.");

            if (await _datos.ExisteDniAsync(socio.DNI, excluirSocioId).ConfigureAwait(false))
                throw new ReglaNegocioException("Ya existe un socio con el DNI " + socio.DNI + ".");
        }

        private static bool EmailValido(string email)
        {
            try
            {
                var direccion = new MailAddress(email);
                return direccion.Address == email;
            }
            catch (System.FormatException)
            {
                return false;
            }
        }
    }
}
