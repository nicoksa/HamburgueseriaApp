using HamburgueseriaApp.Data;
using HamburgueseriaApp.Helpers;
using System.Windows;
using System.Windows.Input;
using System.IO;

namespace HamburgueseriaApp.ViewModels;

public class MainViewModel : ObservableObject
{
    public NuevoPedidoViewModel NuevoPedidoVM { get; } = new();
    public ProductosViewModel ProductosVM { get; } = new();
    public VentasViewModel VentasVM { get; } = new();
    public EstadisticasViewModel EstadisticasVM { get; } = new();
    public ICommand BackupCommand { get; }

    private object _vistaActual;
    public object VistaActual
    {
        get => _vistaActual;
        set => SetProperty(ref _vistaActual, value);
    }

    private string _tituloActual = "Nuevo pedido";
    public string TituloActual
    {
        get => _tituloActual;
        set => SetProperty(ref _tituloActual, value);
    }

    public ICommand IrANuevoPedidoCommand { get; }
    public ICommand IrAProductosCommand { get; }
    public ICommand IrAVentasCommand { get; }
    public ICommand IrAEstadisticasCommand { get; }

    public MainViewModel()
    {
        _vistaActual = NuevoPedidoVM;

        IrANuevoPedidoCommand = new RelayCommand(_ =>
        {
            NuevoPedidoVM.CargarProductos();
            VistaActual = NuevoPedidoVM;
            TituloActual = "Nuevo pedido";
        });

        IrAProductosCommand = new RelayCommand(_ =>
        {
            ProductosVM.Cargar();
            VistaActual = ProductosVM;
            TituloActual = "Productos";
        });

        IrAVentasCommand = new RelayCommand(_ =>
        {
            VentasVM.Cargar();
            VistaActual = VentasVM;
            TituloActual = "Ventas";
        });

        IrAEstadisticasCommand = new RelayCommand(_ =>
        {
            EstadisticasVM.Cargar();
            VistaActual = EstadisticasVM;
            TituloActual = "Estadísticas";
        });

        BackupCommand = new RelayCommand(_ => HacerBackup());
    }


    private void HacerBackup()
    {
        var dialogo = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"bigburger_backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
            Filter = "Base de datos (*.db)|*.db",
            Title = "Guardar backup de la base de datos"
        };

        if (dialogo.ShowDialog() != true)
            return;

        try
        {
            File.Copy(AppDbContext.DbPath, dialogo.FileName, overwrite: true);
            MessageBox.Show("Backup guardado correctamente.", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo hacer el backup: " + ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}
