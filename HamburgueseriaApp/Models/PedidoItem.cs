namespace HamburgueseriaApp.Models;

public class PedidoItem
{
    public int Id { get; set; }
    public int PedidoId { get; set; }

    public int ProductoId { get; set; }

    // Se guardan copiados al momento de la venta para conservar el historial
    // aunque el producto cambie de nombre o precio más adelante.
    public string NombreProducto { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }

    public decimal Subtotal => PrecioUnitario * Cantidad;

    public string? Observaciones { get; set; }

    public bool TieneObservaciones => !string.IsNullOrWhiteSpace(Observaciones);

    /// <summary>Variante real vendida (Simple, Doble, Triple...). Null en productos sin variantes o ventas anteriores.</summary>
    public string? Variante { get; set; }
}
