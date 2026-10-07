using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Views
{
    public partial class DevolucionesView : UserControl
    {
        private readonly PrestamoNegocio _negocio = new PrestamoNegocio();

        public DevolucionesView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await Interfaz.EjecutarAsync(CargarAsync);
        }

        private async Task CargarAsync()
        {
            dgPendientes.ItemsSource = await _negocio.ListarPendientesAsync(txtBuscar.Text);
        }

        private async void Buscar_Click(object sender, RoutedEventArgs e)
        {
            await Interfaz.EjecutarAsync(CargarAsync);
        }

        private async void Devolver_Click(object sender, RoutedEventArgs e)
        {
            var detalle = (DetallePrestamo)((FrameworkElement)sender).DataContext;

            await Interfaz.EjecutarAsync(async () =>
            {
                ResultadoDevolucion resultado = await _negocio.DevolverAsync(detalle.PrestamoId, detalle.LibroId);

                string mensaje = "Libro \"" + detalle.TituloLibro + "\" devuelto correctamente.";
                if (resultado.Multa > 0)
                    mensaje += "\n\nMulta por retraso: " + PrestamoNegocio.FormatearMulta(resultado.Multa) +
                               " (" + resultado.DiasRetraso + " día(s) de retraso)";
                else
                    mensaje += "\n\nDevolución a tiempo: sin multa.";
                Interfaz.Informar(mensaje);

                await CargarAsync();
            });
        }
    }
}
