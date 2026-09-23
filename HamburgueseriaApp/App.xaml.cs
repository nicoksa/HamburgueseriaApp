using System.Text;
using System.Windows;
using HamburgueseriaApp.Data;

namespace HamburgueseriaApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Habilita codepages extendidas (necesario para tildes/ñ al imprimir en ESC/POS)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            using var contexto = new AppDbContext();
            DbInitializer.Inicializar(contexto);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo inicializar la base de datos:\n" + ex.Message,
                "Error crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
