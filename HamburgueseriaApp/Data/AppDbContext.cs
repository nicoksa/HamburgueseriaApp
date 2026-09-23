using HamburgueseriaApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace HamburgueseriaApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItems => Set<PedidoItem>();

    /// <summary>Ruta del archivo .db, ubicado en %LOCALAPPDATA%\HamburgueseriaApp.</summary>
    public static string DbPath { get; }

    static AppDbContext()
    {
        var carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HamburgueseriaApp");
        Directory.CreateDirectory(carpeta);
        DbPath = Path.Combine(carpeta, "hamburgueseria.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(e =>
        {
            e.Property(p => p.Nombre).IsRequired().HasMaxLength(80);
            e.Property(p => p.Tipo).HasConversion<string>();
        });

        modelBuilder.Entity<Pedido>(e =>
        {
            e.Property(p => p.FormaPago).HasConversion<string>();
            e.HasMany(p => p.Items)
             .WithOne()
             .HasForeignKey(i => i.PedidoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PedidoItem>(e =>
        {
            e.Property(i => i.NombreProducto).IsRequired().HasMaxLength(80);
        });
    }
}
