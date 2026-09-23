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
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
}