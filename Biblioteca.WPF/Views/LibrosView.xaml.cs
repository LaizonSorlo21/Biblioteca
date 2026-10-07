using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Views
{
    public partial class LibrosView : UserControl
    {
        private readonly LibroNegocio _negocio = new LibroNegocio();

        public LibrosView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await Interfaz.EjecutarAsync(CargarInicialAsync);
        }

        private async System.Threading.Tasks.Task CargarInicialAsync()
        {
            cmbAutor.ItemsSource = await _negocio.ListarAutoresAsync();
            await CargarLibrosAsync();
        }

        private async System.Threading.Tasks.Task CargarLibrosAsync()
        {
            dgLibros.ItemsSource = await _negocio.ListarAsync(txtBuscar.Text);
        }

        private void LimpiarFormulario()
        {
            dgLibros.SelectedItem = null;
            txtId.Clear();
            txtTitulo.Clear();
            txtIsbn.Clear();
            txtEjemplares.Clear();
            cmbAutor.SelectedIndex = -1;
        }

        private bool TryLeerFormulario(out Libro libro)
        {
            libro = null;
            int ejemplares;
            if (!int.TryParse(txtEjemplares.Text.Trim(), out ejemplares))
            {
                Interfaz.Advertir("Ejemplares debe ser un número entero.");
                return false;
            }

            int id;
            int.TryParse(txtId.Text, out id);
            libro = new Libro
            {
                LibroId = id,
                Titulo = txtTitulo.Text,
                ISBN = txtIsbn.Text,
                AutorId = cmbAutor.SelectedValue == null ? 0 : (int)cmbAutor.SelectedValue,
                Ejemplares = ejemplares
            };
            return true;
        }

        private void dgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var libro = dgLibros.SelectedItem as Libro;
            if (libro == null) return;
            txtId.Text = libro.LibroId.ToString();
            txtTitulo.Text = libro.Titulo;
            txtIsbn.Text = libro.ISBN;
            txtEjemplares.Text = libro.Ejemplares.ToString();
            cmbAutor.SelectedValue = libro.AutorId;
        }

        private async void Buscar_Click(object sender, RoutedEventArgs e)
        {
            await Interfaz.EjecutarAsync(CargarLibrosAsync);
        }

        private void Nuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
            txtTitulo.Focus();
        }

        private void Limpiar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (txtId.Text.Length > 0)
            {
                Interfaz.Advertir("Hay un libro seleccionado. Use \"Actualizar\" o pulse \"Nuevo\" para registrar otro.");
                return;
            }

            Libro libro;
            if (!TryLeerFormulario(out libro)) return;

            await Interfaz.EjecutarAsync(async () =>
            {
                await _negocio.InsertarAsync(libro);
                Interfaz.Informar("Libro registrado correctamente.");
                LimpiarFormulario();
                await CargarLibrosAsync();
            });
        }

        private async void Actualizar_Click(object sender, RoutedEventArgs e)
        {
            Libro libro;
            if (!TryLeerFormulario(out libro)) return;

            await Interfaz.EjecutarAsync(async () =>
            {
                await _negocio.ActualizarAsync(libro);
                Interfaz.Informar("Libro actualizado correctamente.");
                LimpiarFormulario();
                await CargarLibrosAsync();
            });
        }

        private async void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            var libro = dgLibros.SelectedItem as Libro;
            if (libro == null)
            {
                Interfaz.Advertir("Seleccione el libro que desea dar de baja.");
                return;
            }
            if (!Interfaz.Confirmar("¿Dar de baja el libro \"" + libro.Titulo + "\"?")) return;

            await Interfaz.EjecutarAsync(async () =>
            {
                await _negocio.DarDeBajaAsync(libro.LibroId);
                Interfaz.Informar("Libro dado de baja.");
                LimpiarFormulario();
                await CargarLibrosAsync();
            });
        }
    }
}
