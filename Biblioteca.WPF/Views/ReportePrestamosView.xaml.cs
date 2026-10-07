using System;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Views
{
    public partial class ReportePrestamosView : UserControl
    {
        private readonly PrestamoNegocio _negocio = new PrestamoNegocio();

        public ReportePrestamosView()
        {
            InitializeComponent();
            dpDesde.SelectedDate = DateTime.Today.AddDays(-30);
            dpHasta.SelectedDate = DateTime.Today;
        }

        private async void Buscar_Click(object sender, RoutedEventArgs e)
        {
            if (dpDesde.SelectedDate == null || dpHasta.SelectedDate == null)
            {
                Interfaz.Advertir("Seleccione la fecha desde y la fecha hasta.");
                return;
            }

            DateTime desde = dpDesde.SelectedDate.Value;
            DateTime hasta = dpHasta.SelectedDate.Value;

            await Interfaz.EjecutarAsync(async () =>
            {
                dgReporte.ItemsSource = await _negocio.ReportePorFechasAsync(desde, hasta);
            });
        }
    }
}
