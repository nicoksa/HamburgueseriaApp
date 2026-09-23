using System.Runtime.InteropServices;

namespace HamburgueseriaApp.Services;

/// <summary>
/// Permite enviar bytes crudos (RAW) directamente a una impresora de Windows,
/// necesario para mandar comandos ESC/POS a una impresora térmica.
/// </summary>
public static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string? pDocName;
        [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string? pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    /// <summary>Envía un array de bytes crudos a la impresora indicada.</summary>
    public static void EnviarBytes(string nombreImpresora, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(nombreImpresora))
            throw new InvalidOperationException("No hay una impresora configurada.");

        if (!OpenPrinter(nombreImpresora, out IntPtr hPrinter, IntPtr.Zero))
            throw new InvalidOperationException($"No se pudo abrir la impresora '{nombreImpresora}'.");

        try
        {
            var info = new DOCINFOA
            {
                pDocName = "Hamburguesería - Comprobante",
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, info))
                throw new InvalidOperationException("No se pudo iniciar el documento de impresión.");

            try
            {
                if (!StartPagePrinter(hPrinter))
                    throw new InvalidOperationException("No se pudo iniciar la página de impresión.");

                IntPtr punteroNoManejado = Marshal.AllocCoTaskMem(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, punteroNoManejado, bytes.Length);
                    if (!WritePrinter(hPrinter, punteroNoManejado, bytes.Length, out _))
                        throw new InvalidOperationException("No se pudo escribir en la impresora.");
                }
                finally
                {
                    Marshal.FreeCoTaskMem(punteroNoManejado);
                }

                EndPagePrinter(hPrinter);
            }
            finally
            {
                EndDocPrinter(hPrinter);
            }
        }
        finally
        {
            ClosePrinter(hPrinter);
        }
    }

    /// <summary>Nombre de la impresora predeterminada configurada en Windows.</summary>
    public static string? ObtenerImpresoraPredeterminada()
    {
        try
        {
            return new System.Drawing.Printing.PrinterSettings().PrinterName;
        }
        catch
        {
            return null;
        }
    }
}
