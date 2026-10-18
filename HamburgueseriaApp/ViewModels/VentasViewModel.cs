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
    public ICommand AnularCommand { get; }

    public VentasViewModel()
    {
        ReimprimirCommand = new RelayCommand(_ => Reimprimir(), _ => Seleccionado is { Anulado: false });
        FiltrarHoyCommand = new RelayCommand(_ => { Desde = DateTime.Today; Hasta = DateTime.Today; });
        VerDetalleCommand = new RelayCommand(_ => VerDetalle(), _ => Seleccionado != null);
        AnularCommand = new RelayCommand(_ => Anular(), _ => Seleccionado is { Anulado: false });
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


    private void Anular()
    {
        if (Seleccionado == null || Seleccionado.Anulado) return;

        var numero = Seleccionado.NumeroPedido;
        var id = Seleccionado.Id;

        var confirmar = MessageBox.Show(
            $"¿Anular el pedido Nº {numero} por ${Seleccionado.Total.ToString("N0", new System.Globalization.CultureInfo("es-AR"))}?\n\nEsta acción no se puede deshacer.",
            "Confirmar anulación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmar != MessageBoxResult.Yes) return;
 
        var dialogo = new ObservacionItemWindow($"Motivo de anulación (Pedido Nº {numero})", null);
        if (dialogo.ShowDialog() != true) return;

        try
        {
            using var ctx = new AppDbContext();
            var pedido = ctx.Pedidos.Find(id);
            if (pedido == null || pedido.Anulado) return;

            pedido.Anulado = true;
            pedido.FechaAnulacion = DateTime.Now;
            pedido.MotivoAnulacion = string.IsNullOrWhiteSpace(dialogo.Resultado) ? null : dialogo.Resultado.Trim();
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo anular: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Cargar();
    }
}
