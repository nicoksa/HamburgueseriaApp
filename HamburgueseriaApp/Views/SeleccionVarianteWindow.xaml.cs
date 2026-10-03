using System.Windows;
using HamburgueseriaApp.Models;

namespace HamburgueseriaApp.Views;

public partial class SeleccionVarianteWindow : Window
{
    public VarianteProducto? Seleccion { get; private set; }

    public SeleccionVarianteWindow(Producto producto)
    {
        InitializeComponent();
        TxtProducto.Text = producto.Nombre;
        ListaVariantes.ItemsSource = producto.ObtenerVariantes();
    }

    private void Variante_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is VarianteProducto v)
        {
            Seleccion = v;
            DialogResult = true;
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}