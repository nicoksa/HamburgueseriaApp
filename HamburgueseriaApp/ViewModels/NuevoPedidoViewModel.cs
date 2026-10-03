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

    private string? _nombreCliente;
    public string? NombreCliente
    {
        get => _nombreCliente;
        set => SetProperty(ref _nombreCliente, value);
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

    private TipoPedido _tipoPedidoSeleccionado = TipoPedido.Retiro;
    public TipoPedido TipoPedidoSeleccionado
    {
        get => _tipoPedidoSeleccionado;
        set { if (SetProperty(ref _tipoPedidoSeleccionado, value)) OnPropertyChanged(nameof(EsEnvio)); }
    }

    public bool EsEnvio => TipoPedidoSeleccionado == TipoPedido.Envio;

    private string? _direccion;
    public string? Direccion
    {
        get => _direccion;
        set => SetProperty(ref _direccion, value);
    }

    /// <summary>La vista lo asigna para mostrar el diálogo de variantes. Devuelve null si se cancela.</summary>
    public Func<Producto, VarianteProducto?>? SolicitarVariante { get; set; }



    public NuevoPedidoViewModel()
    {
        AgregarProductoCommand = new RelayCommand(p => { if (p is Producto prod) SeleccionarYAgregar(prod); });
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

    private void SeleccionarYAgregar(Producto producto)
    {
        if (!producto.TieneVariantes)
        {
            AgregarProducto(producto, null);
            return;
        }

        var variantes = producto.ObtenerVariantes();

        // Una sola opción disponible: se agrega directo, sin preguntar.
        if (variantes.Count == 1)
        {
            AgregarProducto(producto, variantes[0]);
            return;
        }

        if (SolicitarVariante == null) return;
        var elegida = SolicitarVariante(producto);
        if (elegida != null)
            AgregarProducto(producto, elegida);
    }

    private void AgregarProducto(Producto producto, VarianteProducto? variante)
    {
        // La versión simple conserva el nombre original ("Hércules"); las demás suman el sufijo ("Hércules Doble").
        var nombre = variante is null || variante.EsSimple ? producto.Nombre : $"{producto.Nombre} {variante.Nombre}";
        var precio = variante?.Precio ?? producto.Precio;

        // Se agrupa por producto Y variante, así "Hércules" y "Hércules Doble" son renglones distintos.
        var existente = Items.FirstOrDefault(i => i.ProductoId == producto.Id && i.Nombre == nombre);
        if (existente != null)
        {
            existente.Cantidad++;
        }
        else
        {
            var vm = new PedidoItemViewModel(producto, nombre, precio);
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
                NombreCliente = string.IsNullOrWhiteSpace(NombreCliente) ? null : NombreCliente.Trim(),
                TipoPedido = TipoPedidoSeleccionado,
                Direccion = TipoPedidoSeleccionado == TipoPedido.Envio && !string.IsNullOrWhiteSpace(Direccion)
                            ? Direccion.Trim()
                            : null,
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
        NombreCliente = string.Empty;
        FormaPagoSeleccionada = FormaPago.Efectivo;
        TipoPedidoSeleccionado = TipoPedido.Retiro;
        Direccion = string.Empty;
        RecalcularTotal();
    }
}
