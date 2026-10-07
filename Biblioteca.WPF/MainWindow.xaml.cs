using System.Windows;
using System.Windows.Controls;
using Biblioteca.WPF.Views;

namespace Biblioteca.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            contenido.Content = new InicioView();
        }

        // Se crea una vista nueva en cada navegación para que siempre cargue datos actualizados.
        private void Navegar_Click(object sender, RoutedEventArgs e)
        {
            switch ((string)((Button)sender).Tag)
            {
                case "Libros": contenido.Content = new LibrosView(); break;
                case "Socios": contenido.Content = new SociosView(); break;
                case "Prestamos": contenido.Content = new PrestamosView(); break;
                case "Devoluciones": contenido.Content = new DevolucionesView(); break;
                case "Reporte": contenido.Content = new ReportePrestamosView(); break;
                default: contenido.Content = new InicioView(); break;
            }
        }
    }
}
