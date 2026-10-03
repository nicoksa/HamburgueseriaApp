using HamburgueseriaApp.Models;
using HamburgueseriaApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HamburgueseriaApp.Views;

public partial class NuevoPedidoView : UserControl
{
    private NuevoPedidoViewModel? _vmSuscripto;

    public NuevoPedidoView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (_vmSuscripto != null)
            {
                _vmSuscripto.Error -= MostrarError;
                _vmSuscripto.PedidoConfirmado -= MostrarConfirmacion;
                _vmSuscripto.SolicitarVariante = null;
                _vmSuscripto.SolicitarObservacionItem = null;
            }

            if (e.NewValue is NuevoPedidoViewModel vm)
            {
                vm.Error += MostrarError;
                vm.PedidoConfirmado += MostrarConfirmacion;
                vm.SolicitarVariante = ElegirVariante;
                vm.SolicitarObservacionItem = PedirObservacionItem;
                _vmSuscripto = vm;
            }
        };
    }

    private void MostrarError(string mensaje) =>
        MessageBox.Show(mensaje, "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void MostrarConfirmacion(string mensaje) =>
        MessageBox.Show(mensaje, "Pedido cobrado", MessageBoxButton.OK, MessageBoxImage.Information);

    private VarianteProducto? ElegirVariante(Producto producto)
    {
        var dialogo = new SeleccionVarianteWindow(producto) { Owner = Window.GetWindow(this) };
        return dialogo.ShowDialog() == true ? dialogo.Seleccion : null;
    }

    private string? PedirObservacionItem(string producto, string? actual)
    {
        var d = new ObservacionItemWindow(producto, actual) { Owner = Window.GetWindow(this) };
        return d.ShowDialog() == true ? d.Resultado : null;
    }
}
