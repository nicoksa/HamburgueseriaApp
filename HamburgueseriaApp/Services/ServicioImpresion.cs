using System.IO;
using System.Text;
using HamburgueseriaApp.Models;

namespace HamburgueseriaApp.Services;

/// <summary>
/// Genera y envía a la impresora térmica de 80mm el ticket del cliente
/// y, a continuación, la comanda de cocina, usando comandos ESC/POS.
/// </summary>
public class ServicioImpresion
{
    private const byte ESC = 0x1B;
    private const byte GS = 0x1D;
    private const int ANCHO_COLUMNAS = 32; // aprox. en fuente normal para 80mm

    /// <summary>Nombre de impresora a usar. Si es null, se usa la predeterminada de Windows.</summary>
    public string? NombreImpresora { get; set; }

    public void ImprimirPedido(Pedido pedido)
    {
        var impresora = NombreImpresora ?? RawPrinterHelper.ObtenerImpresoraPredeterminada();
        if (string.IsNullOrWhiteSpace(impresora))
            throw new InvalidOperationException(
                "No se encontró ninguna impresora predeterminada en Windows. " +
                "Configurá la impresora térmica como predeterminada e intentá de nuevo.");

        RawPrinterHelper.EnviarBytes(impresora, ConstruirTicket(pedido));
        RawPrinterHelper.EnviarBytes(impresora, ConstruirComanda(pedido));
    }

    // ---------------------------------------------------------------
    // TICKET DEL CLIENTE
    // ---------------------------------------------------------------
    private byte[] ConstruirTicket(Pedido pedido)
    {
        var enc = ObtenerEncoding();
        using var ms = new MemoryStream();
        void Texto(string s) { var b = enc.GetBytes(s); ms.Write(b, 0, b.Length); }
        void Cmd(params byte[] b) => ms.Write(b, 0, b.Length);
        void Linea() => Texto(new string('-', ANCHO_COLUMNAS) + "\n");

        Cmd(ESC, (byte)'@');            // reset impresora
        Cmd(ESC, (byte)'a', 1);          // centrado
        Cmd(ESC, (byte)'!', 0x30);       // doble ancho + doble alto
        Texto("BIG BURGER\n");
        Cmd(ESC, (byte)'!', 0x00);       // normal
        Texto($"Pedido Nº {pedido.NumeroPedido}\n");
        Texto($"{pedido.Fecha:dd/MM/yyyy HH:mm}\n");
        Linea();

        Cmd(ESC, (byte)'a', 0);          // izquierda
        foreach (var item in pedido.Items)
            Texto($"{item.Cantidad} {item.NombreProducto}\n");

        if (!string.IsNullOrWhiteSpace(pedido.Observaciones))
        {
            Linea();
            Texto("Obs: " + pedido.Observaciones + "\n");
        }

        Linea();
        Cmd(ESC, (byte)'a', 1);
        Cmd(ESC, (byte)'!', 0x10);       // doble alto
        Texto($"TOTAL ${pedido.Total:N0}\n");
        Cmd(ESC, (byte)'!', 0x00);
        Texto($"Forma de pago: {DescripcionFormaPago(pedido.FormaPago)}\n");
        Texto("\n");
        Texto("Gracias por su compra!\n");
        Texto("\n\n\n");

        Cmd(GS, (byte)'V', 1);           // corte de papel (parcial)
        return ms.ToArray();
    }

    // ---------------------------------------------------------------
    // COMANDA DE COCINA
    // ---------------------------------------------------------------
    private byte[] ConstruirComanda(Pedido pedido)
    {
        var enc = ObtenerEncoding();
        using var ms = new MemoryStream();
        void Texto(string s) { var b = enc.GetBytes(s); ms.Write(b, 0, b.Length); }
        void Cmd(params byte[] b) => ms.Write(b, 0, b.Length);
        void Linea() => Texto(new string('-', ANCHO_COLUMNAS) + "\n");

        Cmd(ESC, (byte)'@');
        Cmd(ESC, (byte)'a', 1);
        Cmd(ESC, (byte)'!', 0x30);
        Texto("COCINA\n");
        Cmd(ESC, (byte)'!', 0x00);
        Cmd(ESC, (byte)'E', 1);          // negrita ON
        Texto($"PEDIDO {pedido.NumeroPedido}\n");
        Cmd(ESC, (byte)'E', 0);          // negrita OFF
        Texto($"{pedido.Fecha:HH:mm}\n");
        Linea();

        Cmd(ESC, (byte)'a', 0);
        Cmd(ESC, (byte)'!', 0x10);       // doble alto: se lee de lejos en cocina
        foreach (var item in pedido.Items)
            Texto($"{item.Cantidad} {item.NombreProducto.ToUpperInvariant()}\n");
        Cmd(ESC, (byte)'!', 0x00);

        if (!string.IsNullOrWhiteSpace(pedido.Observaciones))
        {
            Linea();
            Cmd(ESC, (byte)'E', 1);
            Texto("OBSERVACIONES\n");
            Cmd(ESC, (byte)'E', 0);
            Texto(pedido.Observaciones + "\n");
        }

        Texto("\n\n\n");
        Cmd(GS, (byte)'V', 1);
        return ms.ToArray();
    }

    private static Encoding ObtenerEncoding()
    {
        // CP850: soporta tildes y ñ en la gran mayoría de impresoras térmicas ESC/POS.
        try { return Encoding.GetEncoding(850); }
        catch { return Encoding.ASCII; }
    }

    private static string DescripcionFormaPago(FormaPago formaPago) => formaPago switch
    {
        FormaPago.Efectivo => "Efectivo",
        FormaPago.Transferencia => "Transferencia",
        _ => formaPago.ToString()
    };
}
