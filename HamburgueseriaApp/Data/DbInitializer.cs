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

        var productos = new List<Producto>();

        // ---------- Hamburguesas clásicas: Simple $13.000 / Doble $16.000 / Triple $19.000 ----------
        string[] clasicas =
        {
            "Ironman", "Capitán América", "Hulk", "Flash", "Batman", "Wolverine",
            "Namor", "Thor", "Tormenta", "Superman", "Robin", "Rusty", "Aquaman", "Hércules"
        };
        foreach (var nombre in clasicas)
        {
            productos.Add(new()
            {
                Nombre = nombre,
                Precio = 13000,
                PrecioDoble = 16000,
                PrecioTriple = 19000,
                Tipo = TipoProducto.Hamburguesa,
                Activo = true
            });
        }

        // Cheeseburger tiene precios propios: S $10.000 / D $14.000 / T $18.000
        productos.Add(new()
        {
            Nombre = "Cheeseburger",
            Precio = 10000,
            PrecioDoble = 14000,
            PrecioTriple = 18000,
            Tipo = TipoProducto.Hamburguesa,
            Activo = true
        });

        // ---------- House Edition: precio único $18.000 ----------
        foreach (var nombre in new[] { "Gorgory", "Barney", "Marge", "Montgomery" })
        {
            productos.Add(new()
            {
                Nombre = nombre,
                Precio = 18000,
                Tipo = TipoProducto.Hamburguesa,
                Activo = true
            });
        }

        // Veggie: $14.000
        productos.Add(new() { Nombre = "Veggie", Precio = 14000, Tipo = TipoProducto.Hamburguesa, Activo = true });

        // ---------- Papas ----------
        productos.Add(new() { Nombre = "Papas fritas", Precio = 9000, Tipo = TipoProducto.Papas, Activo = true });
        productos.Add(new() { Nombre = "Papas fritas con cheddar", Precio = 12000, Tipo = TipoProducto.Papas, Activo = true });

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
