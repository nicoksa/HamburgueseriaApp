using HamburgueseriaApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HamburgueseriaApp.Data;

/// <summary>
/// Crea la base SQLite si no existe y carga productos de ejemplo la primera vez.
/// </summary>
public static class DbInitializer
{
    public static void Inicializar(AppDbContext contexto)
    {
        // Crea el archivo .db y el esquema si todavía no existen.
        contexto.Database.EnsureCreated();
        MigrarEsquema(contexto);

        if (contexto.Productos.Any())
            return;

        var productos = new List<Producto>
        {
            new() { Nombre = "Hércules",  Precio = 8500, Tipo = TipoProducto.Hamburguesa, Activo = true },
            new() { Nombre = "Batman",    Precio = 8800, Tipo = TipoProducto.Hamburguesa, Activo = true },
            new() { Nombre = "Wolverine", Precio = 9200, Tipo = TipoProducto.Hamburguesa, Activo = true },
            new() { Nombre = "Aquaman",   Precio = 8700, Tipo = TipoProducto.Hamburguesa, Activo = true },

            new() { Nombre = "Papas",              Precio = 2500, Tipo = TipoProducto.Papas, Activo = true },
            new() { Nombre = "Papas con cheddar",  Precio = 3200, Tipo = TipoProducto.Papas, Activo = true },

            new() { Nombre = "Extra cheddar", Precio = 800,  Tipo = TipoProducto.Extra, Activo = true },
            new() { Nombre = "Extra bacon",   Precio = 1200, Tipo = TipoProducto.Extra, Activo = true },
            new() { Nombre = "Extra medallón", Precio = 3500, Tipo = TipoProducto.Extra, Activo = true },

            new() { Nombre = "Coca-Cola", Precio = 2000, Tipo = TipoProducto.Bebida, Activo = true },
            new() { Nombre = "Sprite",    Precio = 2000, Tipo = TipoProducto.Bebida, Activo = true },
            new() { Nombre = "Agua",      Precio = 1500, Tipo = TipoProducto.Bebida, Activo = true },
        };

        contexto.Productos.AddRange(productos);
        contexto.SaveChanges();
    }


    /// <summary>EnsureCreated no modifica tablas existentes, así que agregamos a mano las columnas nuevas.</summary>
    private static void MigrarEsquema(AppDbContext ctx)
    {
        AgregarColumnaSiFalta(ctx, "Pedidos", "TipoPedido", "TEXT NOT NULL DEFAULT 'Retiro'");
        AgregarColumnaSiFalta(ctx, "Pedidos", "Direccion", "TEXT NULL");
        AgregarColumnaSiFalta(ctx, "Productos", "PrecioDoble", "TEXT NULL");
        AgregarColumnaSiFalta(ctx, "Productos", "PrecioTriple", "TEXT NULL");
        AgregarColumnaSiFalta(ctx, "Productos", "PrecioCuadruple", "TEXT NULL");
        AgregarColumnaSiFalta(ctx, "PedidoItems", "Variante", "TEXT NULL");
        AgregarColumnaSiFalta(ctx, "PedidoItems", "Observaciones", "TEXT NULL");
        AgregarColumnaSiFalta(ctx, "Pedidos", "HoraEntrega", "TEXT NULL");
    }

    private static void AgregarColumnaSiFalta(AppDbContext ctx, string tabla, string columna, string definicion)
    {
        var conn = ctx.Database.GetDbConnection();
        bool abrir = conn.State != ConnectionState.Open;
        if (abrir) conn.Open();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info('{tabla}')";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                if (string.Equals(r.GetString(1), columna, StringComparison.OrdinalIgnoreCase))
                    return; // ya existe
        }
        finally
        {
            if (abrir) conn.Close();
        }

        ctx.Database.ExecuteSqlRaw($"ALTER TABLE {tabla} ADD COLUMN {columna} {definicion}");
    }
}
