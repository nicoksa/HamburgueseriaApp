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

    /// <summary>(nombre del producto, observación actual) -> nueva observación. null = cancelado.</summary>
    public Func<string, string?, string?>? SolicitarObservacionItem { get; set; }
    public ICommand EditarObservacionItemCommand { get; }

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
    private string? _horaEntrega;
    public string? HoraEntrega
    {
        get => _horaEntrega;
        set => SetProperty(ref _horaEntrega, value);
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
        EditarObservacionItemCommand = new RelayCommand(p => { if (p is PedidoItemViewModel it) EditarObservacionItem(it); });

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
        var existente = BuscarIgual(producto.Id, nombre, variante?.Nombre, null);
        if (existente != null) existente.Cantidad++;
        else AgregarItem(new PedidoItemViewModel(producto, nombre, precio, variante?.Nombre));
        RecalcularTotal();

    }

    private void RecalcularTotal() => Total = Items.Sum(i => i.Subtotal);

    private void CobrarEImprimir()
    {

        if (!TryNormalizarHora(HoraEntrega, out var horaNormalizada))
        {
            Error?.Invoke("La hora no es válida.");
            return;
        }

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
                HoraEntrega = horaNormalizada,
                Items = Items.Select(i => new PedidoItem
                {
                    ProductoId = i.ProductoId,
                    NombreProducto = i.Nombre,
                    PrecioUnitario = i.PrecioUnitario,
                    Variante = i.Variante,
                    Observaciones = i.Observaciones,
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
        HoraEntrega = string.Empty;
    }

    private static bool TryNormalizarHora(string? texto, out string? hora)
    {
        hora = null;
        if (string.IsNullOrWhiteSpace(texto)) return true; // vacío = sin hora

        var t = texto.Trim().ToLowerInvariant()
                     .Replace("hs", "").Replace('h', ':').Replace('.', ':').Replace(',', ':')
                     .Replace(' ', ':')
                     .Trim(':');

        // "21::30" (por espacios dobles) pasa a "21:30"
        while (t.Contains("::")) t = t.Replace("::", ":");

        int h = 0, m = 0;
        if (t.Contains(':'))
        {
            var partes = t.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length is < 1 or > 2 || !int.TryParse(partes[0], out h)) return false;
            if (partes.Length == 2 && !int.TryParse(partes[1], out m)) return false;
        }
        else if (t.Length <= 2)
        {
            if (!int.TryParse(t, out h)) return false;
        }
        else if (t.Length <= 4 && int.TryParse(t, out var n))
        {
            h = n / 100;
            m = n % 100;
        }
        else return false;

        if (h is < 0 or > 23 || m is < 0 or > 59) return false;
        hora = $"{h:00}:{m:00}";
        return true;
    }


    private static string? Normalizar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private PedidoItemViewModel? BuscarIgual(int productoId, string nombre, string? variante, string? obs, PedidoItemViewModel? excluir = null)
        => Items.FirstOrDefault(i => i != excluir && i.ProductoId == productoId
                                     && i.Nombre == nombre
                                     && i.Variante == variante
                                     && Normalizar(i.Observaciones) == Normalizar(obs));

    private void AgregarItem(PedidoItemViewModel vm, int? indice = null)
    {
        vm.PropertyChanged += (_, __) => RecalcularTotal();
        if (indice.HasValue) Items.Insert(indice.Value, vm); else Items.Add(vm);
    }

    private void EditarObservacionItem(PedidoItemViewModel item)
    {
        if (SolicitarObservacionItem == null) return;

        var resultado = SolicitarObservacionItem(item.Nombre, item.Observaciones);
        if (resultado == null) return;                    // canceló
        var nueva = Normalizar(resultado);
        if (nueva == Normalizar(item.Observaciones)) return;

        var igual = BuscarIgual(item.ProductoId, item.Nombre, item.Variante, nueva, item);

        if (item.Cantidad > 1)
        {
            // Se separa UNA unidad con la nota nueva; el resto queda como estaba.
            item.Cantidad--;
            if (igual != null) igual.Cantidad++;
            else AgregarItem(new PedidoItemViewModel(item.ProductoId, item.Nombre, item.PrecioUnitario, nueva, item.Variante),
                  Items.IndexOf(item) + 1);
        }
        else if (igual != null)
        {
            // Quedó idéntico a otro renglón: se fusionan.
            igual.Cantidad++;
            Items.Remove(item);
        }
        else
        {
            item.Observaciones = nueva;
        }
        RecalcularTotal();
    }
}
