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

        CmbTipo.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (CmbTipo.IsDropDownOpen)
                return;

            CmbTipo.IsDropDownOpen = true;
            e.Handled = true;
        };


        CmbTipo.ItemsSource = Enum.GetValues(typeof(TipoProducto));
        Title = producto.Id == 0 ? "Nuevo producto" : "Editar producto";

        TxtNombre.Text = producto.Nombre;
        TxtPrecio.Text = producto.Precio > 0 ? producto.Precio.ToString(CultureInfo.InvariantCulture) : "";
        CmbTipo.SelectedItem = producto.Tipo;
        ChkActivo.IsChecked = producto.Activo;

        TxtPrecioDoble.Text = FormatearPrecio(producto.PrecioDoble);
        TxtPrecioTriple.Text = FormatearPrecio(producto.PrecioTriple);
        TxtPrecioCuadruple.Text = FormatearPrecio(producto.PrecioCuadruple);

        // Los precios de variantes solo se muestran para hamburguesas
        CmbTipo.SelectionChanged += (_, __) => ActualizarPanelVariantes();
        ActualizarPanelVariantes();
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNombre.Text))
        {
            MessageBox.Show("Ingresá un nombre para el producto.", "Falta información", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbTipo.SelectedItem is not TipoProducto tipo)
        {
            MessageBox.Show("Seleccioná un tipo de producto.", "Falta información", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 1) Variantes (solo aplican a hamburguesas)
        decimal? doble = null, triple = null, cuadruple = null;
        if (tipo == TipoProducto.Hamburguesa &&
            (!TryParsePrecioOpcional(TxtPrecioDoble.Text, out doble) ||
             !TryParsePrecioOpcional(TxtPrecioTriple.Text, out triple) ||
             !TryParsePrecioOpcional(TxtPrecioCuadruple.Text, out cuadruple)))
        {
            MessageBox.Show("Revisá los precios de las variantes (solo números, o vacío si no existe).",
                "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 2) Precio simple: puede quedar vacío solo si es hamburguesa y tiene al menos una variante
        bool hayVariante = doble.HasValue || triple.HasValue || cuadruple.HasValue;
        decimal precio = 0;

        if (string.IsNullOrWhiteSpace(TxtPrecio.Text))
        {
            if (!(tipo == TipoProducto.Hamburguesa && hayVariante))
            {
                MessageBox.Show("Ingresá el precio, o cargá al menos una variante (doble, triple o cuádruple).",
                    "Precio requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else if (!decimal.TryParse(TxtPrecio.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out precio) || precio < 0)
        {
            MessageBox.Show("Ingresá un precio válido (por ejemplo: 8500).", "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 3) Asignar
        Producto.Nombre = TxtNombre.Text.Trim();
        Producto.Precio = precio;
        Producto.Tipo = tipo;
        Producto.Activo = ChkActivo.IsChecked ?? true;
        Producto.PrecioDoble = doble;
        Producto.PrecioTriple = triple;
        Producto.PrecioCuadruple = cuadruple;

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ActualizarPanelVariantes()
    {
        PanelVariantes.Visibility = CmbTipo.SelectedItem is TipoProducto.Hamburguesa
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static string FormatearPrecio(decimal? precio)
        => precio.HasValue ? precio.Value.ToString(CultureInfo.InvariantCulture) : "";

    private static bool TryParsePrecioOpcional(string? texto, out decimal? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(texto)) return true;   // vacío = no existe
        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && d >= 0)
        {
            valor = d;
            return true;
        }
        return false;
    }
}
