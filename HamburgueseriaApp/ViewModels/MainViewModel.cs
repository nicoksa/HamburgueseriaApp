using System.Windows.Input;
using HamburgueseriaApp.Helpers;

namespace HamburgueseriaApp.ViewModels;

public class MainViewModel : ObservableObject
{
    public NuevoPedidoViewModel NuevoPedidoVM { get; } = new();
    public ProductosViewModel ProductosVM { get; } = new();
    public VentasViewModel VentasVM { get; } = new();
    public EstadisticasViewModel EstadisticasVM { get; } = new();

    private object _vistaActual;
    public object VistaActual
    {
        get => _vistaActual;
        set => SetProperty(ref _vistaActual, value);
    }

    private string _tituloActual = "Nuevo pedido";
    public string TituloActual
    {
        get => _tituloActual;
        set => SetProperty(ref _tituloActual, value);
    }

    public ICommand IrANuevoPedidoCommand { get; }
    public ICommand IrAProductosCommand { get; }
    public ICommand IrAVentasCommand { get; }
    public ICommand IrAEstadisticasCommand { get; }

    public MainViewModel()
    {
        _vistaActual = NuevoPedidoVM;

        IrANuevoPedidoCommand = new RelayCommand(_ =>
        {
            NuevoPedidoVM.CargarProductos();
            VistaActual = NuevoPedidoVM;
            TituloActual = "Nuevo pedido";
        });

        IrAProductosCommand = new RelayCommand(_ =>
        {
            ProductosVM.Cargar();
            VistaActual = ProductosVM;
            TituloActual = "Productos";
        });

        IrAVentasCommand = new RelayCommand(_ =>
        {
            VentasVM.Cargar();
            VistaActual = VentasVM;
            TituloActual = "Ventas";
        });

        IrAEstadisticasCommand = new RelayCommand(_ =>
        {
            EstadisticasVM.Cargar();
            VistaActual = EstadisticasVM;
            TituloActual = "Estadísticas";
        });
    }
}
