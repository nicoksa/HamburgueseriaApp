namespace HamburgueseriaApp.Models;

public class Pedido
{
    public int Id { get; set; }

    /// <summary>Número correlativo visible en el ticket (no depende del Id interno).</summary>
    public int NumeroPedido { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;
    public decimal Total { get; set; }
    public FormaPago FormaPago { get; set; }
    public string? Observaciones { get; set; }

    public string? NombreCliente { get; set; }

    public List<PedidoItem> Items { get; set; } = new();

    public TipoPedido TipoPedido { get; set; } = TipoPedido.Retiro;

    /// <summary>Opcional. Solo tiene sentido si TipoPedido es Envio.</summary>
    public string? Direccion { get; set; }

    /// <summary>Hora pactada de retiro o envío, formato "HH:mm". Opcional.</summary>
    public string? HoraEntrega { get; set; }
}
