namespace HamburgueseriaApp.Models;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }              // precio de la versión simple
    public TipoProducto Tipo { get; set; }
    public bool Activo { get; set; } = true;

    // null = esa variante no existe para este producto
    public decimal? PrecioDoble { get; set; }
    public decimal? PrecioTriple { get; set; }
    public decimal? PrecioCuadruple { get; set; }

    // Propiedades de solo lectura: EF no las mapea.
    public bool TieneVariantes => PrecioDoble.HasValue || PrecioTriple.HasValue || PrecioCuadruple.HasValue;

    /// <summary>Precio 0 = la versión simple no existe (solo si hay otras variantes).</summary>
    public bool TieneSimple => Precio > 0;

    /// <summary>True si es un producto que solo se vende en variantes (sin simple).</summary>
    public bool SinSimple => !TieneSimple && TieneVariantes;

    /// <summary>El precio más bajo disponible, para mostrar "Desde $X" en los botones.</summary>
    public decimal PrecioDesde => ObtenerVariantes().Min(v => v.Precio);

    public List<VarianteProducto> ObtenerVariantes()
    {
        var lista = new List<VarianteProducto>();
        if (TieneSimple || !TieneVariantes)
            lista.Add(new("Simple", Precio, EsSimple: true));
        if (PrecioDoble is decimal d) lista.Add(new("Doble", d));
        if (PrecioTriple is decimal t) lista.Add(new("Triple", t));
        if (PrecioCuadruple is decimal c) lista.Add(new("Cuádruple", c));

        // La primera variante disponible es la "base" del producto: no suma sufijo al nombre.
        // (Si hay simple, es la simple; si no, es la más chica que exista, ej. la doble de la Gorgory.)
        if (lista.Count > 0)
            lista[0] = lista[0] with { EsSimple = true };

        return lista;
    }
}