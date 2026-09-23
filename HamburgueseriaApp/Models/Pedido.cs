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

    public List<PedidoItem> Items { get; set; } = new();
}
