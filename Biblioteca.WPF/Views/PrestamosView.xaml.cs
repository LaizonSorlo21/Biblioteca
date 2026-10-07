using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Views
{
    public partial class PrestamosView : UserControl
    {
        private readonly SocioNegocio _socios = new SocioNegocio();
        private readonly LibroNegocio _libros = new LibroNegocio();
        private readonly PrestamoNegocio _prestamos = new PrestamoNegocio();
        private readonly ObservableCollection<Libro> _seleccion = new ObservableCollection<Libro>();

        public PrestamosView()
        {
            InitializeComponent();
            dgSeleccion.ItemsSource = _seleccion;
            dpFechaPrestamo.SelectedDate = DateTime.Today;
            dpFechaLimite.SelectedDate = DateTime.Today.AddDays(7);
            dpFechaLimite.DisplayDateStart = DateTime.Today;
            Loaded += async (s, e) => await Interfaz.EjecutarAsync(CargarDatosAsync);
        }

        private async Task CargarDatosAsync()
        {
            cmbSocio.ItemsSource = await _socios.ListarActivosAsync();
            dgLibros.ItemsSource = await _libros.ListarActivosAsync();
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            foreach (Libro libro in dgLibros.SelectedItems.Cast<Libro>())
            {
                if (_seleccion.All(l => l.LibroId != libro.LibroId))
                    _seleccion.Add(libro);
            }
        }

        private void Quitar_Click(object sender, RoutedEventArgs e)
        {
            foreach (Libro libro in dgSeleccion.SelectedItems.Cast<Libro>().ToList())
                _seleccion.Remove(libro);
        }

        private async void Registrar_Click(object sender, RoutedEventArgs e)
        {
            if (dpFechaLimite.SelectedDate == null)
            {
                Interfaz.Advertir("Seleccione la fecha límite de devolución.");
                return;
            }

            int socioId = cmbSocio.SelectedValue == null ? 0 : (int)cmbSocio.SelectedValue;
            DateTime fechaLimite = dpFechaLimite.SelectedDate.Value;
            var ids = _seleccion.Select(l => l.LibroId).ToList();

            await Interfaz.EjecutarAsync(async () =>
            {
                int prestamoId = await _prestamos.RegistrarAsync(socioId, ids, fechaLimite);
                Interfaz.Informar("Préstamo N° " + prestamoId + " registrado correctamente.");
                _seleccion.Clear();
                cmbSocio.SelectedIndex = -1;
                await CargarDatosAsync();
            });
        }
    }
}
