using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Views
{
    public partial class SociosView : UserControl
    {
        private readonly SocioNegocio _negocio = new SocioNegocio();

        public SociosView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await Interfaz.EjecutarAsync(CargarSociosAsync);
        }

        private async Task CargarSociosAsync()
        {
            dgSocios.ItemsSource = await _negocio.ListarAsync(txtBuscar.Text);
        }

        private void LimpiarFormulario()
        {
            dgSocios.SelectedItem = null;
            txtId.Clear();
            txtDni.Clear();
            txtNombre.Clear();
            txtEmail.Clear();
        }

        private Socio LeerFormulario()
        {
            int id;
            int.TryParse(txtId.Text, out id);
            return new Socio { SocioId = id, DNI = txtDni.Text, Nombre = txtNombre.Text, Email = txtEmail.Text };
        }

        private void dgSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var socio = dgSocios.SelectedItem as Socio;
            if (socio == null) return;
            txtId.Text = socio.SocioId.ToString();
            txtDni.Text = socio.DNI;
            txtNombre.Text = socio.Nombre;
            txtEmail.Text = socio.Email;
        }

        private async void Buscar_Click(object sender, RoutedEventArgs e)
        {
            await Interfaz.EjecutarAsync(CargarSociosAsync);
        }

        private void Nuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
            txtDni.Focus();
        }

        private void Limpiar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (txtId.Text.Length > 0)
            {
                Interfaz.Advertir("Hay un socio seleccionado. Use \"Actualizar\" o pulse \"Nuevo\" para registrar otro.");
                return;
            }

            Socio socio = LeerFormulario();
            await Interfaz.EjecutarAsync(async () =>
            {
                await _negocio.InsertarAsync(socio);
                Interfaz.Informar("Socio registrado correctamente.");
                LimpiarFormulario();
                await CargarSociosAsync();
            });
        }

        private async void Actualizar_Click(object sender, RoutedEventArgs e)
        {
            Socio socio = LeerFormulario();
            await Interfaz.EjecutarAsync(async () =>
            {
                await _negocio.ActualizarAsync(socio);
                Interfaz.Informar("Socio actualizado correctamente.");
                LimpiarFormulario();
                await CargarSociosAsync();
            });
        }

        private async void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            var socio = dgSocios.SelectedItem as Socio;
            if (socio == null)
            {
                Interfaz.Advertir("Seleccione el socio que desea dar de baja.");
                return;
            }
            if (!Interfaz.Confirmar("¿Dar de baja al socio \"" + socio.Nombre + "\"?")) return;

            await Interfaz.EjecutarAsync(async () =>
            {
                await _negocio.DarDeBajaAsync(socio.SocioId);
                Interfaz.Informar("Socio dado de baja.");
                LimpiarFormulario();
                await CargarSociosAsync();
            });
        }
    }
}
