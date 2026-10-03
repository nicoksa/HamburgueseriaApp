using System.Windows;

namespace HamburgueseriaApp.Views;

public partial class ObservacionItemWindow : Window
{
    public string Resultado { get; private set; } = string.Empty;

    public ObservacionItemWindow(string producto, string? actual)
    {
        InitializeComponent();
        TxtProducto.Text = producto;
        TxtObservacion.Text = actual ?? "";

        Loaded += (_, __) =>
        {
            TxtObservacion.Focus();
            TxtObservacion.CaretIndex = TxtObservacion.Text.Length;
        };
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        Resultado = TxtObservacion.Text;
        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}