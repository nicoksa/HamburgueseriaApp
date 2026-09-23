# Big Burger — Sistema de pedidos de escritorio

Aplicación de escritorio para Windows (.NET 8 + WPF + EF Core + SQLite) pensada para una
hamburguesería chica con una sola caja y una impresora térmica de 80mm.

## Cómo abrirlo

1. Abrí `HamburgueseriaApp.sln` con **Visual Studio 2022** (con la carga de trabajo
   ".NET desktop development" instalada).
2. Visual Studio va a restaurar automáticamente los paquetes NuGet (EF Core Sqlite,
   EF Core Design, System.Text.Encoding.CodePages) la primera vez que compiles.
3. Presioná F5. La app abre directo en **Nuevo Pedido**.

No requiere SQL Server, internet ni login. La base SQLite se crea sola la primera vez en:
`%LOCALAPPDATA%\HamburgueseriaApp\hamburgueseria.db`

## Estructura

```
HamburgueseriaApp/
├── Models/        Producto, Pedido, PedidoItem, enums
├── Data/          AppDbContext (EF Core + SQLite) y datos semilla
├── Services/       ServicioImpresion (ESC/POS) y RawPrinterHelper (envío RAW a la impresora)
├── Helpers/       ObservableObject, RelayCommand, converters de XAML
├── ViewModels/     Un ViewModel por pantalla (MVVM simple, sin frameworks extra)
└── Views/         XAML + code-behind mínimo de cada pantalla
```

## Notas sobre la base de datos

Se usa `Database.EnsureCreated()` en vez de migraciones formales de EF Core, porque
crea el archivo `.db` y el esquema automáticamente sin pasos extra — ideal para una
instalación sin conocimientos técnicos. Si en el futuro necesitás versionar cambios de
esquema (por ejemplo, agregar una columna), lo más prolijo es migrar a
`Database.Migrate()` corriendo `dotnet ef migrations add NombreCambio` desde la carpeta
del proyecto.

## Impresión térmica (ESC/POS)

- `ServicioImpresion` arma dos comprobantes (ticket + comanda) como bytes ESC/POS crudos
  y los manda, uno después del otro, a la **impresora predeterminada de Windows** usando
  `RawPrinterHelper` (P/Invoke a `winspool.drv`).
- Como el local tiene una sola impresora térmica USB, simplemente configurala como
  **impresora predeterminada** en Windows (Configuración → Impresoras y escáneres) y la
  app la va a usar sola, sin que haya que elegir nada en pantalla.
- Si el ticket sale con acentos/ñ mal impresos, es porque el firmware de tu impresora usa
  una codepage distinta a CP850. En ese caso avisame el modelo y ajusto
  `ObtenerEncoding()` en `ServicioImpresion.cs`.
- El corte de papel usa `GS V 1` (corte parcial), el comando ESC/POS más compatible.
  Si tu impresora no corta, puede que necesite `GS V 0` (corte total) — es un cambio de
  una línea en `ServicioImpresion.cs`.

## Qué probar primero

1. **Productos**: ya vienen cargados Hércules, Batman, Wolverine, Aquaman, papas, extras
   y bebidas de ejemplo. Editalos con tus productos y precios reales.
2. **Nuevo pedido**: tocá un par de productos, escribí una observación ("sin cebolla"),
   elegí forma de pago y probá **COBRAR E IMPRIMIR** con la impresora conectada.
3. **Ventas**: vas a ver el pedido recién cobrado, con opción de reimprimir.
4. **Estadísticas**: totales del día, producto más vendido, hora pico y gráficos simples
   de barras (hamburguesas, bebidas y ventas por hora).

## Decisiones de diseño (por qué así)

- **MVVM manual, sin CommunityToolkit.Mvvm ni frameworks de terceros**: para un proyecto
  de este tamaño, agregar generadores de código externos suma una capa de "magia" que no
  aporta velocidad de desarrollo real y sí puede complicar el build en una PC sin mucho
  soporte técnico. `ObservableObject` y `RelayCommand` son ~40 líneas cada uno.
- **Gráficos como barras propias (Border + MultiBinding), no una librería de charts**:
  evita una dependencia pesada para algo que visualmente es un ranking simple.
- **Impresión RAW vía P/Invoke en vez de imprimir un documento**: es el método estándar
  y más confiable para impresoras térmicas ESC/POS; controla el corte de papel y evita
  que Windows intente "formatear" el ticket como una hoja A4.
- **Eliminar vs. desactivar productos**: si un producto ya tiene ventas asociadas, no se
  borra (rompería el historial), se desactiva. Si nunca se vendió, se borra directo.
