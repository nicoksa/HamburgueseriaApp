using System.Windows;
using HamburgueseriaApp.Models;

namespace HamburgueseriaApp.Views;

public partial class DetallePedidoWindow : Window
{
    public DetallePedidoWindow(Pedido pedido)
    {
        InitializeComponent();
        DataContext = pedido;

        if (string.IsNullOrWhiteSpace(pedido.Observaciones))
            PanelObservaciones.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(pedido.NombreCliente))
            TxtCliente.Visibility = Visibility.Collapsed;

        var tipo = pedido.TipoPedido == TipoPedido.Envio ? "Envío" : "Retiro";
        TxtTipo.Text = string.IsNullOrWhiteSpace(pedido.HoraEntrega) ? tipo : $"{tipo} · {pedido.HoraEntrega} hs";

        if (pedido.TipoPedido == TipoPedido.Envio && !string.IsNullOrWhiteSpace(pedido.Direccion))
            TxtDireccion.Text = "Dirección: " + pedido.Direccion;
        else
            TxtDireccion.Visibility = Visibility.Collapsed;
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
}