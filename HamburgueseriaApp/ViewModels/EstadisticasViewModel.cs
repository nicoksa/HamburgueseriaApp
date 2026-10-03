using System.Collections.ObjectModel;
using System.Windows.Input;
using HamburgueseriaApp.Data;
using HamburgueseriaApp.Helpers;
using HamburgueseriaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace HamburgueseriaApp.ViewModels;

public enum ModoEstadisticas
{
    PorDia,
    PorRango
}

/// <summary>Representa una barra en los gráficos simples basados en cantidades (unidades).</summary>
public class EstadisticaBarra
{
    public string Etiqueta { get; init; } = string.Empty;
    public int Cantidad { get; init; }
}

/// <summary>Igual que EstadisticaBarra pero para montos en pesos (ingresos por categoría, tendencia diaria, etc).</summary>
public class EstadisticaBarraMonto
{
    public string Etiqueta { get; init; } = string.Empty;
    public decimal Monto { get; init; }
}

/// <summary>Una fila de la tendencia de ventas por día, para períodos de más de un día.</summary>
public class VentaDiaria
{
    public DateTime Fecha { get; init; }
    public decimal Total { get; init; }
    public int CantidadPedidos { get; init; }
}

public class ResumenTipoPedido
{
    public string Etiqueta { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal Porcentaje { get; init; }
    public decimal Total { get; init; }
    public decimal TicketPromedio { get; init; }
}

public class EstadisticasViewModel : ObservableObject
{
    // ================= SELECTOR DE PERÍODO =================

    private ModoEstadisticas _modo = ModoEstadisticas.PorDia;
    public ModoEstadisticas Modo
    {
        get => _modo;
        set
        {
            if (SetProperty(ref _modo, value))
            {
                OnPropertyChanged(nameof(EsModoDia));
                OnPropertyChanged(nameof(EsModoRango));
                Cargar();
            }
        }
    }

    public bool EsModoDia => Modo == ModoEstadisticas.PorDia;
    public bool EsModoRango => Modo == ModoEstadisticas.PorRango;

    private DateTime _fechaSeleccionada = DateTime.Today;
    public DateTime FechaSeleccionada
    {
        get => _fechaSeleccionada;
        set { if (SetProperty(ref _fechaSeleccionada, value) && EsModoDia) Cargar(); }
    }

    private DateTime _desdeRango = DateTime.Today.AddDays(-6);
    public DateTime DesdeRango
    {
        get => _desdeRango;
        set { if (SetProperty(ref _desdeRango, value) && EsModoRango) Cargar(); }
    }

    private DateTime _hastaRango = DateTime.Today;
    public DateTime HastaRango
    {
        get => _hastaRango;
        set { if (SetProperty(ref _hastaRango, value) && EsModoRango) Cargar(); }
    }

    private string _textoPeriodo = "";
    public string TextoPeriodo { get => _textoPeriodo; private set => SetProperty(ref _textoPeriodo, value); }

    // ================= KPIs PRINCIPALES =================

    private decimal _totalVendido;
    public decimal TotalVendido { get => _totalVendido; private set => SetProperty(ref _totalVendido, value); }

    private int _cantidadPedidos;
    public int CantidadPedidos { get => _cantidadPedidos; private set => SetProperty(ref _cantidadPedidos, value); }

    private decimal _ticketPromedio;
    public decimal TicketPromedio { get => _ticketPromedio; private set => SetProperty(ref _ticketPromedio, value); }

    private decimal _totalEfectivo;
    public decimal TotalEfectivo { get => _totalEfectivo; private set => SetProperty(ref _totalEfectivo, value); }

    private decimal _totalTransferencia;
    public decimal TotalTransferencia { get => _totalTransferencia; private set => SetProperty(ref _totalTransferencia, value); }

    // ================= DATOS DESTACADOS =================

    private string _productoMasVendido = "-";
    public string ProductoMasVendido { get => _productoMasVendido; private set => SetProperty(ref _productoMasVendido, value); }

    private string _horaPico = "-";
    public string HoraPico { get => _horaPico; private set => SetProperty(ref _horaPico, value); }

    private string _mejorDia = "-";
    public string MejorDia { get => _mejorDia; private set => SetProperty(ref _mejorDia, value); }

    private int _totalUnidadesVendidas;
    public int TotalUnidadesVendidas { get => _totalUnidadesVendidas; private set => SetProperty(ref _totalUnidadesVendidas, value); }

    private decimal _variacionPorcentual;
    public decimal VariacionPorcentual { get => _variacionPorcentual; private set => SetProperty(ref _variacionPorcentual, value); }

    private bool? _variacionEsPositiva = true;
    public bool? VariacionEsPositiva { get => _variacionEsPositiva; private set => SetProperty(ref _variacionEsPositiva, value); }

    private string _variacionTexto = "";
    public string VariacionTexto { get => _variacionTexto; private set => SetProperty(ref _variacionTexto, value); }

    // ================= GRÁFICOS =================

    public ObservableCollection<EstadisticaBarra> TopProductos { get; } = new();
    private int _maxTopProductos = 1;
    public int MaxTopProductos { get => _maxTopProductos; private set => SetProperty(ref _maxTopProductos, value); }

    public ObservableCollection<EstadisticaBarraMonto> IngresosPorCategoria { get; } = new();
    private decimal _maxIngresoCategoria = 1;
    public decimal MaxIngresoCategoria { get => _maxIngresoCategoria; private set => SetProperty(ref _maxIngresoCategoria, value); }

    public ObservableCollection<VentaDiaria> VentasPorDia { get; } = new();
    private decimal _maxVentaDiaria = 1;
    public decimal MaxVentaDiaria { get => _maxVentaDiaria; private set => SetProperty(ref _maxVentaDiaria, value); }

    private bool _mostrarVentasPorDia;
    public bool MostrarVentasPorDia { get => _mostrarVentasPorDia; private set => SetProperty(ref _mostrarVentasPorDia, value); }

    public ObservableCollection<EstadisticaBarra> VentasPorHora { get; } = new();
    private int _maxPorHora = 1;
    public int MaxPorHora { get => _maxPorHora; private set => SetProperty(ref _maxPorHora, value); }

    public ObservableCollection<EstadisticaBarraMonto> VentasPorDiaSemana { get; } = new();
    private decimal _maxDiaSemana = 1;
    public decimal MaxDiaSemana { get => _maxDiaSemana; private set => SetProperty(ref _maxDiaSemana, value); }

    private bool _mostrarPorDiaSemana;
    public bool MostrarPorDiaSemana { get => _mostrarPorDiaSemana; private set => SetProperty(ref _mostrarPorDiaSemana, value); }


    public ObservableCollection<ResumenTipoPedido> TipoPedidoResumen { get; } = new();

    public ObservableCollection<EstadisticaBarraMonto> RankingIngresos { get; } = new();
    private decimal _maxRankingIngresos = 1;
    public decimal MaxRankingIngresos { get => _maxRankingIngresos; private set => SetProperty(ref _maxRankingIngresos, value); }

    // ================= COMANDOS =================

    public ICommand ActualizarCommand { get; }
    public ICommand HoyCommand { get; }
    public ICommand AyerCommand { get; }
    public ICommand Ultimos7DiasCommand { get; }
    public ICommand Ultimos30DiasCommand { get; }
    public ICommand EsteMesCommand { get; }
    public ICommand MesAnteriorCommand { get; }

    public EstadisticasViewModel()
    {
        ActualizarCommand = new RelayCommand(_ => Cargar());

        HoyCommand = new RelayCommand(_ =>
        {
            Modo = ModoEstadisticas.PorDia;
            FechaSeleccionada = DateTime.Today;
            Cargar();
        });

        AyerCommand = new RelayCommand(_ =>
        {
            Modo = ModoEstadisticas.PorDia;
            FechaSeleccionada = DateTime.Today.AddDays(-1);
            Cargar();
        });

        Ultimos7DiasCommand = new RelayCommand(_ =>
        {
            Modo = ModoEstadisticas.PorRango;
            DesdeRango = DateTime.Today.AddDays(-6);
            HastaRango = DateTime.Today;
            Cargar();
        });

        Ultimos30DiasCommand = new RelayCommand(_ =>
        {
            Modo = ModoEstadisticas.PorRango;
            DesdeRango = DateTime.Today.AddDays(-29);
            HastaRango = DateTime.Today;
            Cargar();
        });

        EsteMesCommand = new RelayCommand(_ =>
        {
            Modo = ModoEstadisticas.PorRango;
            DesdeRango = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            HastaRango = DateTime.Today;
            Cargar();
        });

        MesAnteriorCommand = new RelayCommand(_ =>
        {
            var primerDiaMesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Modo = ModoEstadisticas.PorRango;
            DesdeRango = primerDiaMesActual.AddMonths(-1);
            HastaRango = primerDiaMesActual.AddDays(-1);
            Cargar();
        });

        Cargar();
    }

    public void Cargar()
    {
        using var ctx = new AppDbContext();

        DateTime desde = (EsModoDia ? FechaSeleccionada : DesdeRango).Date;
        DateTime hastaExclusive = (EsModoDia ? FechaSeleccionada : HastaRango).Date.AddDays(1);
        if (hastaExclusive <= desde) hastaExclusive = desde.AddDays(1); // salvaguarda si "Hasta" quedó antes que "Desde"

        int totalDias = Math.Max(1, (hastaExclusive - desde).Days);

        TextoPeriodo = EsModoDia
            ? $"Estadísticas del {desde:dd/MM/yyyy}"
            : $"Estadísticas del {desde:dd/MM/yyyy} al {hastaExclusive.AddDays(-1):dd/MM/yyyy}";

        var pedidosPeriodo = ctx.Pedidos
            .Include(p => p.Items)
            .Where(p => p.Fecha >= desde && p.Fecha < hastaExclusive)
            .ToList();

        TotalVendido = pedidosPeriodo.Sum(p => p.Total);
        CantidadPedidos = pedidosPeriodo.Count;
        TicketPromedio = CantidadPedidos > 0 ? TotalVendido / CantidadPedidos : 0;
        TotalEfectivo = pedidosPeriodo.Where(p => p.FormaPago == FormaPago.Efectivo).Sum(p => p.Total);
        TotalTransferencia = pedidosPeriodo.Where(p => p.FormaPago == FormaPago.Transferencia).Sum(p => p.Total);

        var itemsPeriodo = pedidosPeriodo.SelectMany(p => p.Items).ToList();
        TotalUnidadesVendidas = itemsPeriodo.Sum(i => i.Cantidad);

        var productosPorId = ctx.Productos.ToDictionary(p => p.Id);

        // ---------- Agrupado por producto (simple/doble/triple se suman juntos) ----------
        var porProducto = itemsPeriodo
            .GroupBy(i => i.ProductoId)
            .Select(g =>
            {
                productosPorId.TryGetValue(g.Key, out var prod);
                return new
                {
                    Nombre = prod?.Nombre ?? g.First().NombreProducto,
                    Tipo = prod?.Tipo,
                    Unidades = g.Sum(i => i.Cantidad),
                    Ingresos = g.Sum(i => i.Subtotal)
                };
            })
            .ToList();

        // ---------- Top productos por unidades vendidas ----------
        var topProductos = porProducto
            .OrderByDescending(x => x.Unidades)
            .Take(6)
            .Select(x => new EstadisticaBarra { Etiqueta = x.Nombre, Cantidad = x.Unidades })
            .ToList();
        ProductoMasVendido = topProductos.FirstOrDefault()?.Etiqueta ?? "-";
        TopProductos.Clear();
        foreach (var t in topProductos) TopProductos.Add(t);
        MaxTopProductos = Math.Max(1, topProductos.Count > 0 ? topProductos.Max(t => t.Cantidad) : 1);

        // ---------- Ranking por ingresos ----------
        // Para solo hamburguesas, agregá .Where(x => x.Tipo == TipoProducto.Hamburguesa) antes del OrderByDescending
        var ranking = porProducto
            .OrderByDescending(x => x.Ingresos)
            .Take(8)
            .Select(x => new EstadisticaBarraMonto { Etiqueta = x.Nombre, Monto = x.Ingresos })
            .ToList();
        RankingIngresos.Clear();
        foreach (var r in ranking) RankingIngresos.Add(r);
        MaxRankingIngresos = Math.Max(1, ranking.Count > 0 ? ranking.Max(r => r.Monto) : 1);

        // ---------- Comparación contra el período anterior de igual duración ----------
        DateTime desdeAnterior = desde.AddDays(-totalDias);
        DateTime hastaAnteriorExclusive = desde;
        decimal totalAnterior = ctx.Pedidos
            .Where(p => p.Fecha >= desdeAnterior && p.Fecha < hastaAnteriorExclusive)
            .Select(p => p.Total)
            .ToList()
            .Sum();

        if (totalAnterior == 0)
        {
            VariacionPorcentual = 0;
            VariacionEsPositiva = null; // sin dato: se pinta en gris
            VariacionTexto = "Sin datos previos";
        }
        else
        {
            VariacionPorcentual = Math.Round((TotalVendido - totalAnterior) / totalAnterior * 100, 1);
            VariacionEsPositiva = VariacionPorcentual >= 0;
            VariacionTexto = $"{(VariacionPorcentual >= 0 ? "+" : "")}{VariacionPorcentual:0.#}% vs. período anterior";
        }

        // ---------- Envío vs Retiro ----------
        TipoPedidoResumen.Clear();
        foreach (var tipo in new[] { TipoPedido.Retiro, TipoPedido.Envio })
        {
            var grupo = pedidosPeriodo.Where(p => p.TipoPedido == tipo).ToList();
            var total = grupo.Sum(p => p.Total);
            TipoPedidoResumen.Add(new ResumenTipoPedido
            {
                Etiqueta = tipo == TipoPedido.Envio ? "ENVÍO" : "RETIRO",
                Cantidad = grupo.Count,
                Porcentaje = pedidosPeriodo.Count > 0 ? Math.Round(100m * grupo.Count / pedidosPeriodo.Count, 0) : 0,
                Total = total,
                TicketPromedio = grupo.Count > 0 ? total / grupo.Count : 0
            });
        }

        // ---------- Hora pico (agregado de todo el período) ----------
        var picoGrupo = pedidosPeriodo.GroupBy(p => p.Fecha.Hour).OrderByDescending(g => g.Count()).FirstOrDefault();
        HoraPico = picoGrupo != null ? $"{picoGrupo.Key:00}:00 - {(picoGrupo.Key + 1):00}:00" : "-";

        // ---------- Mejor día del período ----------
        if (totalDias > 1 && pedidosPeriodo.Count > 0)
        {
            var mejorDiaGrupo = pedidosPeriodo
                .GroupBy(p => p.Fecha.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(p => p.Total) })
                .OrderByDescending(g => g.Total)
                .First();
            MejorDia = $"{mejorDiaGrupo.Fecha:dd/MM} (${mejorDiaGrupo.Total:N0})";
        }
        else
        {
            MejorDia = "-";
        }

        // ---------- Ingresos por categoría (qué rubro genera más plata) ----------
        var ingresosCategoria = itemsPeriodo
            .Select(i => new
            {
                Item = i,
                Tipo = productosPorId.TryGetValue(i.ProductoId, out var p) ? p.Tipo : (TipoProducto?)null
            })
            .GroupBy(x => x.Tipo)
            .Select(g => new EstadisticaBarraMonto { Etiqueta = DescribirTipo(g.Key), Monto = g.Sum(x => x.Item.Subtotal) })
            .OrderByDescending(g => g.Monto)
            .ToList();
        IngresosPorCategoria.Clear();
        foreach (var c in ingresosCategoria) IngresosPorCategoria.Add(c);
        MaxIngresoCategoria = Math.Max(1, ingresosCategoria.Count > 0 ? ingresosCategoria.Max(c => c.Monto) : 1);

        // ---------- Tendencia de ventas por día (solo si el período abarca más de un día) ----------
        MostrarVentasPorDia = totalDias > 1;
        VentasPorDia.Clear();
        if (MostrarVentasPorDia)
        {
            var porDia = pedidosPeriodo
                .GroupBy(p => p.Fecha.Date)
                .Select(g => new VentaDiaria { Fecha = g.Key, Total = g.Sum(p => p.Total), CantidadPedidos = g.Count() })
                .OrderBy(v => v.Fecha)
                .ToList();
            foreach (var v in porDia) VentasPorDia.Add(v);
            MaxVentaDiaria = Math.Max(1, porDia.Count > 0 ? porDia.Max(v => v.Total) : 1);
        }
        else
        {
            MaxVentaDiaria = 1;
        }

        // ---------- Ventas por hora (agregado del período completo, útil para organizar turnos) ----------
        var porHora = pedidosPeriodo
            .GroupBy(p => p.Fecha.Hour)
            .Select(g => new EstadisticaBarra { Etiqueta = $"{g.Key:00}:00", Cantidad = g.Count() })
            .OrderBy(g => g.Etiqueta)
            .ToList();
        VentasPorHora.Clear();
        foreach (var h in porHora) VentasPorHora.Add(h);
        MaxPorHora = Math.Max(1, porHora.Count > 0 ? porHora.Max(h => h.Cantidad) : 1);

        // ---------- Ventas por día de la semana (útil para reforzar personal los días fuertes) ----------
        string[] nombresDias = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
        var porDiaSemana = pedidosPeriodo
            .GroupBy(p => p.Fecha.DayOfWeek)
            .Select(g => new EstadisticaBarraMonto { Etiqueta = nombresDias[(int)g.Key], Monto = g.Sum(p => p.Total) })
            .OrderByDescending(v => v.Monto)
            .ToList();
        VentasPorDiaSemana.Clear();
        foreach (var d in porDiaSemana) VentasPorDiaSemana.Add(d);
        MaxDiaSemana = Math.Max(1, porDiaSemana.Count > 0 ? porDiaSemana.Max(d => d.Monto) : 1);
        MostrarPorDiaSemana = totalDias > 1;
    }

    private static string DescribirTipo(TipoProducto? tipo) => tipo switch
    {
        TipoProducto.Hamburguesa => "Hamburguesas",
        TipoProducto.Papas => "Papas",
        TipoProducto.Bebida => "Bebidas",
        TipoProducto.Extra => "Extras",
        _ => "Otros"
    };
}