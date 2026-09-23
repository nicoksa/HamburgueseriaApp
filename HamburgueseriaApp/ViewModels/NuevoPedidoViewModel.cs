using System.Collections.ObjectModel;
using System.Windows.Input;
using HamburgueseriaApp.Data;
using HamburgueseriaApp.Helpers;
using HamburgueseriaApp.Models;
using HamburgueseriaApp.Services;
using Microsoft.EntityFrameworkCore;

namespace HamburgueseriaApp.ViewModels;

public class NuevoPedidoViewModel : ObservableObject
{
    private readonly ServicioImpresion _servicioImpresion = new();

    public ObservableCollection<Producto> Hamburguesas { get; } = new();
    public ObservableCollection<Producto> PapasYExtras { get; } = new();
    public ObservableCollection<Producto> Bebidas { get; } = new();

    public ObservableCollection<PedidoItemViewModel> Items { get; } = new();

    private string? _observaciones;
    public string? Observaciones
    {
        get => _observaciones;
        set => SetProperty(ref _observaciones, value);
    }

    private decimal _total;
    public decimal Total
    {
        get => _total;
        private set => SetProperty(ref _total, value);
    }

    private FormaPago _formaPagoSeleccionada = FormaPago.Efectivo;
    public FormaPago FormaPagoSeleccionada
    {
        get => _formaPagoSeleccionada;
        set => SetProperty(ref _formaPagoSeleccionada, value);
    }

    public bool HayItems => Items.Count > 0;

    public ICommand AgregarProductoCommand { get; }
    public ICommand IncrementarCommand { get; }
    public ICommand DecrementarCommand { get; }
    public ICommand EliminarItemCommand { get; }
    public ICommand CobrarEImprimirCommand { get; }

    /// <summary>Se dispara con un mensaje de error para que la vista lo muestre.</summary>
    public event Action<string>? Error;
    /// <summary>Se dispara con un mensaje de éxito luego de cobrar.</summary>
    public event Action<string>? PedidoConfirmado;

    public NuevoPedidoViewModel()
    {
        AgregarProductoCommand = new RelayCommand(p => { if (p is Producto prod) AgregarProducto(prod); });
        IncrementarCommand = new RelayCommand(p => { if (p is PedidoItemViewModel it) { it.Cantidad++; RecalcularTotal(); } });
        DecrementarCommand = new RelayCommand(p =>
        {
            if (p is not PedidoItemViewModel it) return;
            if (it.Cantidad <= 1) Items.Remove(it);
            else it.Cantidad--;
            RecalcularTotal();
        });
        EliminarItemCommand = new RelayCommand(p => { if (p is PedidoItemViewModel it) { Items.Remove(it); RecalcularTotal(); } });
        CobrarEImprimirCommand = new RelayCommand(_ => CobrarEImprimir(), _ => Items.Count > 0);

        Items.CollectionChanged += (_, __) =>
        {
            RecalcularTotal();
            OnPropertyChanged(nameof(HayItems));
        };

        CargarProductos();
    }

    public void CargarProductos()
    {
        using var ctx = new AppDbContext();
        var productos = ctx.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();

        Hamburguesas.Clear();
        PapasYExtras.Clear();
        Bebidas.Clear();

        foreach (var p in productos)
        {
            switch (p.Tipo)
            {
                case TipoProducto.Hamburguesa: Hamburguesas.Add(p); break;
                case TipoProducto.Bebida: Bebidas.Add(p); break;
                default: PapasYExtras.Add(p); break; // Papas y Extras comparten sección
            }
        }
    }

    private void AgregarProducto(Producto producto)
    {
        var existente = Items.FirstOrDefault(i => i.ProductoId == producto.Id);
        if (existente != null)
        {
            existente.Cantidad++;
        }
        else
        {
            var vm = new PedidoItemViewModel(producto);
            vm.PropertyChanged += (_, __) => RecalcularTotal();
            Items.Add(vm);
        }
        RecalcularTotal();
    }

    private void RecalcularTotal() => Total = Items.Sum(i => i.Subtotal);

    private void CobrarEImprimir()
    {
        Pedido pedido;
        try
        {
            using var ctx = new AppDbContext();
            int siguienteNumero = ctx.Pedidos.Any() ? ctx.Pedidos.Max(p => p.NumeroPedido) + 1 : 1;

            pedido = new Pedido
            {
                NumeroPedido = siguienteNumero,
                Fecha = DateTime.Now,
                Total = Total,
                FormaPago = FormaPagoSeleccionada,
                Observaciones = Observaciones,
                Items = Items.Select(i => new PedidoItem
                {
                    ProductoId = i.ProductoId,
                    NombreProducto = i.Nombre,
                    PrecioUnitario = i.PrecioUnitario,
                    Cantidad = i.Cantidad
                }).ToList()
            };

            ctx.Pedidos.Add(pedido);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Error?.Invoke("No se pudo guardar el pedido: " + ex.Message);
            return;
        }

        try
        {
            _servicioImpresion.ImprimirPedido(pedido);
        }
        catch (Exception ex)
        {
            Error?.Invoke("El pedido se guardó, pero hubo un error al imprimir: " + ex.Message);
        }

        int numero = pedido.NumeroPedido;
        LimpiarPedido();
        PedidoConfirmado?.Invoke($"Pedido Nº {numero} cobrado correctamente.");
    }

    public void LimpiarPedido()
    {
        Items.Clear();
        Observaciones = string.Empty;
        FormaPagoSeleccionada = FormaPago.Efectivo;
        RecalcularTotal();
    }
}
