using System.Globalization;
using System.Windows;
using HamburgueseriaApp.Models;

namespace HamburgueseriaApp.Views;

public partial class ProductoEditWindow : Window
{
    public Producto Producto { get; }

    public ProductoEditWindow(Producto producto)
    {
        InitializeComponent();
        Producto = producto;

        CmbTipo.ItemsSource = Enum.GetValues(typeof(TipoProducto));
        Title = producto.Id == 0 ? "Nuevo producto" : "Editar producto";

        TxtNombre.Text = producto.Nombre;
        TxtPrecio.Text = producto.Precio > 0 ? producto.Precio.ToString(CultureInfo.InvariantCulture) : "";
        CmbTipo.SelectedItem = producto.Tipo;
        ChkActivo.IsChecked = producto.Activo;
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNombre.Text))
        {
            MessageBox.Show("Ingresá un nombre para el producto.", "Falta información", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPrecio.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var precio) || precio < 0)
        {
            MessageBox.Show("Ingresá un precio válido (por ejemplo: 8500).", "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbTipo.SelectedItem is not TipoProducto tipo)
        {
            MessageBox.Show("Seleccioná un tipo de producto.", "Falta información", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Producto.Nombre = TxtNombre.Text.Trim();
        Producto.Precio = precio;
        Producto.Tipo = tipo;
        Producto.Activo = ChkActivo.IsChecked ?? true;

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
