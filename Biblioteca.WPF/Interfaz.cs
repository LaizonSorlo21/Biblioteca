using System;
using System.Threading.Tasks;
using System.Windows;
using Biblioteca.Negocio;

namespace Biblioteca.WPF
{
    /// <summary>Utilidades de interfaz: mensajes y manejo uniforme de errores (sin tipos de SqlClient).</summary>
    internal static class Interfaz
    {
        public static async Task EjecutarAsync(Func<Task> accion)
        {
            try
            {
                await accion();
            }
            catch (ReglaNegocioException ex)
            {
                Advertir(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void Advertir(string mensaje)
        {
            MessageBox.Show(mensaje, "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void Informar(string mensaje)
        {
            MessageBox.Show(mensaje, "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static bool Confirmar(string mensaje)
        {
            return MessageBox.Show(mensaje, "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
