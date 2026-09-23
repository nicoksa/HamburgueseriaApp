using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HamburgueseriaApp.Data;
using HamburgueseriaApp.Helpers;
using HamburgueseriaApp.Models;
using HamburgueseriaApp.Views;

namespace HamburgueseriaApp.ViewModels;

public class ProductosViewModel : ObservableObject
{
    public ObservableCollection<Producto> Productos { get; } = new();

    private Producto? _seleccionado;
    public Producto? Seleccionado
    {
        get => _seleccionado;
        set => SetProperty(ref _seleccionado, value);
    }

    public ICommand AgregarCommand { get; }
    public ICommand EditarCommand { get; }
    public ICommand EliminarCommand { get; }

    public ProductosViewModel()
    {
        AgregarCommand = new RelayCommand(_ => Agregar());
        EditarCommand = new RelayCommand(_ => Editar(), _ => Seleccionado != null);
        EliminarCommand = new RelayCommand(_ => Eliminar(), _ => Seleccionado != null);
        Cargar();
    }

    public void Cargar()
    {
        using var ctx = new AppDbContext();
        var lista = ctx.Productos.OrderBy(p => p.Tipo).ThenBy(p => p.Nombre).ToList();
        Productos.Clear();
        foreach (var p in lista) Productos.Add(p);
    }

    private void Agregar()
    {
        var dialogo = new ProductoEditWindow(new Producto { Activo = true });
        if (dialogo.ShowDialog() == true)
        {
            using var ctx = new AppDbContext();
            ctx.Productos.Add(dialogo.Producto);
            ctx.SaveChanges();
            Cargar();
        }
    }

    private void Editar()
    {
        if (Seleccionado == null) return;

        var copia = new Producto
        {
            Id = Seleccionado.Id,
            Nombre = Seleccionado.Nombre,
            Precio = Seleccionado.Precio,
            Tipo = Seleccionado.Tipo,
            Activo = Seleccionado.Activo
        };

        var dialogo = new ProductoEditWindow(copia);
        if (dialogo.ShowDialog() == true)
        {
            using var ctx = new AppDbContext();
            ctx.Productos.Update(dialogo.Producto);
            ctx.SaveChanges();
            Cargar();
        }
    }

    private void Eliminar()
    {
        if (Seleccionado == null) return;

        var respuesta = MessageBox.Show(
            $"¿Eliminar '{Seleccionado.Nombre}'?",
            "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (respuesta != MessageBoxResult.Yes) return;

        using var ctx = new AppDbContext();
        var producto = ctx.Productos.Find(Seleccionado.Id);
        if (producto == null) return;

        // Si el producto ya tiene ventas registradas, se desactiva en vez de borrarlo
        // para no perder el historial de pedidos anteriores.
        bool tieneVentas = ctx.PedidoItems.Any(i => i.ProductoId == producto.Id);
        if (tieneVentas)
            producto.Activo = false;
        else
            ctx.Productos.Remove(producto);

        ctx.SaveChanges();
        Cargar();
    }
}
