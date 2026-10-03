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
            }

            if (e.NewValue is NuevoPedidoViewModel vm)
            {
                vm.Error += MostrarError;
                vm.PedidoConfirmado += MostrarConfirmacion;
                vm.SolicitarVariante = ElegirVariante;
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
}
