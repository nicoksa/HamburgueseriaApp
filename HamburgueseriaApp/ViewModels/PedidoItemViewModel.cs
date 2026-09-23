using HamburgueseriaApp.Helpers;
using HamburgueseriaApp.Models;

namespace HamburgueseriaApp.ViewModels;

public class PedidoItemViewModel : ObservableObject
{
    public int ProductoId { get; }
    public string Nombre { get; }
    public decimal PrecioUnitario { get; }

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

    public decimal Subtotal => PrecioUnitario * Cantidad;

    public PedidoItemViewModel(Producto producto)
    {
        ProductoId = producto.Id;
        Nombre = producto.Nombre;
        PrecioUnitario = producto.Precio;
        _cantidad = 1;
    }
}
