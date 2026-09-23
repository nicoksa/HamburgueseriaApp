using System.Windows;
using System.Windows.Controls;
using HamburgueseriaApp.ViewModels;

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
            }

            if (e.NewValue is NuevoPedidoViewModel vm)
            {
                vm.Error += MostrarError;
                vm.PedidoConfirmado += MostrarConfirmacion;
                _vmSuscripto = vm;
            }
        };
    }

    private void MostrarError(string mensaje) =>
        MessageBox.Show(mensaje, "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void MostrarConfirmacion(string mensaje) =>
        MessageBox.Show(mensaje, "Pedido cobrado", MessageBoxButton.OK, MessageBoxImage.Information);
}
