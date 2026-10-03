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

        // Ticket y comanda salen idénticos: se construye una sola vez y se envía dos veces.
        var comprobante = ConstruirTicket(pedido);
        RawPrinterHelper.EnviarBytes(impresora, comprobante); // copia del cliente
        RawPrinterHelper.EnviarBytes(impresora, comprobante); // copia de cocina / entrega
    }

    // ---------------------------------------------------------------
    // TICKET (se imprime dos veces)
    // ---------------------------------------------------------------
    private byte[] ConstruirTicket(Pedido pedido)
    {
        var enc = ObtenerEncoding();
        using var ms = new MemoryStream();
        void Texto(string s) { var b = enc.GetBytes(s); ms.Write(b, 0, b.Length); }
        void Cmd(params byte[] b) => ms.Write(b, 0, b.Length);
        void Linea() => Texto(new string('-', ANCHO_COLUMNAS) + "\n");

        Cmd(ESC, (byte)'@');            // reset impresora
        Cmd(ESC, (byte)'a', 1);         // centrado
        Cmd(ESC, (byte)'!', 0x30);      // doble ancho + doble alto
        Texto("BIG BURGER\n");

        Cmd(ESC, (byte)'!', 0x10);      // doble alto
        Texto($"Pedido Nº {pedido.NumeroPedido}\n");
        if (!string.IsNullOrWhiteSpace(pedido.NombreCliente))
            Texto($"{pedido.NombreCliente}\n");
        Cmd(ESC, (byte)'!', 0x00);      // normal
        Texto($"{pedido.Fecha:dd/MM/yyyy HH:mm}\n");
        Cmd(ESC, (byte)'!', 0x10);
        Cmd(ESC, (byte)'E', 1);
        Texto(DescripcionEntrega(pedido) + "\n");
        Cmd(ESC, (byte)'E', 0);
        Cmd(ESC, (byte)'!', 0x00);
        if (pedido.TipoPedido == TipoPedido.Envio && !string.IsNullOrWhiteSpace(pedido.Direccion))
            foreach (var l in Envolver("Dir: " + pedido.Direccion, ANCHO_COLUMNAS))
                Texto($"{l}\n");
        Linea();

        // Items en doble alto: se leen mejor y no se rompe el ancho de 32 columnas
        Cmd(ESC, (byte)'a', 0);         // izquierda
        foreach (var item in pedido.Items)
        {
            Cmd(ESC, (byte)'!', 0x10);
            foreach (var l in Envolver($"{item.Cantidad} {item.NombreProducto}", ANCHO_COLUMNAS))
                Texto($"{l}\n");

            if (!string.IsNullOrWhiteSpace(item.Observaciones))
            {
                Cmd(ESC, (byte)'!', 0x00);
                Cmd(ESC, (byte)'E', 1);  // negrita ON
                foreach (var l in Envolver(item.Observaciones, ANCHO_COLUMNAS - 5))
                    Texto($"  >> {l}\n");
                Cmd(ESC, (byte)'E', 0);  // negrita OFF
            }
        }
        Cmd(ESC, (byte)'!', 0x00);

        if (!string.IsNullOrWhiteSpace(pedido.Observaciones))
        {
            Linea();
            Cmd(ESC, (byte)'E', 1);
            Texto("OBSERVACIONES\n");
            Cmd(ESC, (byte)'E', 0);
            Cmd(ESC, (byte)'!', 0x10);
            foreach (var l in Envolver(pedido.Observaciones, ANCHO_COLUMNAS))
                Texto($"{l}\n");
            Cmd(ESC, (byte)'!', 0x00);
        }

        Linea();
        Cmd(ESC, (byte)'a', 1);
        Cmd(ESC, (byte)'!', 0x30);      // total bien grande
        Texto($"TOTAL ${pedido.Total:N0}\n");
        Cmd(ESC, (byte)'!', 0x10);
        Texto($"{DescripcionFormaPago(pedido.FormaPago)}\n");
        Cmd(ESC, (byte)'!', 0x00);
        Texto("\n");
        Texto("Gracias por su compra!\n");
        Texto("\n\n\n");

        Cmd(GS, (byte)'V', 1);          // corte de papel (parcial)
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

    private static string DescripcionEntrega(Pedido p)
    {
        var tipo = p.TipoPedido == TipoPedido.Envio ? "ENVÍO" : "RETIRO";
        return string.IsNullOrWhiteSpace(p.HoraEntrega) ? tipo : $"{tipo} {p.HoraEntrega} hs";
    }

    private static IEnumerable<string> Envolver(string texto, int ancho)
    {
        var linea = new StringBuilder();
        foreach (var palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (linea.Length > 0 && linea.Length + 1 + palabra.Length > ancho)
            {
                yield return linea.ToString();
                linea.Clear();
            }
            if (linea.Length > 0) linea.Append(' ');
            linea.Append(palabra);
        }
        if (linea.Length > 0) yield return linea.ToString();
    }
}
