using HamburgueseriaApp.Helpers;
using HamburgueseriaApp.Models;

namespace HamburgueseriaApp.ViewModels;

public class PedidoItemViewModel : ObservableObject
{
    public int ProductoId { get; }
    public string Nombre { get; }
    public decimal PrecioUnitario { get; }

    public string? Variante { get; }

    private int _cantidad;
    public int Cantidad
    {
        get => _cantidad;
        set
        {
            if (value < 1) value = 1;
            if (SetProperty(ref _cantidad, value))
                OnPropertyChanged(nameof(Subtotal));
        }
    }

    private string? _observaciones;
    public string? Observaciones
    {
        get => _observaciones;
        set
        {
            if (SetProperty(ref _observaciones, value))
                OnPropertyChanged(nameof(TieneObservaciones));
        }
    }
    public bool TieneObservaciones => !string.IsNullOrWhiteSpace(Observaciones);

    public decimal Subtotal => PrecioUnitario * Cantidad;

    public PedidoItemViewModel(Producto producto, string? nombre = null, decimal? precio = null, string? variante = null)
          : this(producto.Id, nombre ?? producto.Nombre, precio ?? producto.Precio, null, variante) { }

    public PedidoItemViewModel(int productoId, string nombre, decimal precio, string? observaciones, string? variante = null)
    {
        ProductoId = productoId;
        Nombre = nombre;
        PrecioUnitario = precio;
        Variante = variante;
        _observaciones = observaciones;
        _cantidad = 1;
    }
}
