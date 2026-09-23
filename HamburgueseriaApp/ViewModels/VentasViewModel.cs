using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HamburgueseriaApp.Data;
using HamburgueseriaApp.Helpers;
using HamburgueseriaApp.Models;
using HamburgueseriaApp.Services;
using Microsoft.EntityFrameworkCore;
using HamburgueseriaApp.Views;

namespace HamburgueseriaApp.ViewModels;

public class VentasViewModel : ObservableObject
{
    private readonly ServicioImpresion _servicioImpresion = new();

    public ObservableCollection<Pedido> Pedidos { get; } = new();

    private DateTime _desde = DateTime.Today;
    public DateTime Desde
    {
        get => _desde;
        set { if (SetProperty(ref _desde, value)) Cargar(); }
    }

    private DateTime _hasta = DateTime.Today;
    public DateTime Hasta
    {
        get => _hasta;
        set { if (SetProperty(ref _hasta, value)) Cargar(); }
    }

    private Pedido? _seleccionado;
    public Pedido? Seleccionado
    {
        get => _seleccionado;
        set => SetProperty(ref _seleccionado, value);
    }

    public ICommand ReimprimirCommand { get; }
    public ICommand FiltrarHoyCommand { get; }
    public ICommand VerDetalleCommand { get; }

    public VentasViewModel()
    {
        ReimprimirCommand = new RelayCommand(_ => Reimprimir(), _ => Seleccionado != null);
        FiltrarHoyCommand = new RelayCommand(_ => { Desde = DateTime.Today; Hasta = DateTime.Today; });
        VerDetalleCommand = new RelayCommand(_ => VerDetalle(), _ => Seleccionado != null);
        Cargar();
    }

    public void Cargar()
    {
        using var ctx = new AppDbContext();
        var desde = Desde.Date;
        var hasta = Hasta.Date.AddDays(1);

        var lista = ctx.Pedidos
            .Include(p => p.Items)
            .Where(p => p.Fecha >= desde && p.Fecha < hasta)
            .OrderByDescending(p => p.Fecha)
            .ToList();

        Pedidos.Clear();
        foreach (var p in lista) Pedidos.Add(p);
    }

    private void Reimprimir()
    {
        if (Seleccionado == null) return;
        try
        {
            _servicioImpresion.ImprimirPedido(Seleccionado);
            MessageBox.Show("Ticket y comanda reimpresos.", "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al imprimir: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void VerDetalle()
    {
        if (Seleccionado == null) return;
        var ventana = new DetallePedidoWindow(Seleccionado);
        ventana.ShowDialog();
    }
}
