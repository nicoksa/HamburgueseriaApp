using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HamburgueseriaApp.Helpers;

/// <summary>True -> Collapsed, False -> Visible. Útil para mostrar un mensaje "vacío" cuando SÍ hay datos.</summary>
public class BoolToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>True -> Visible, False -> Collapsed. Complemento de BoolToCollapsedConverter, para mostrar/ocultar
/// secciones enteras según un flag (por ejemplo, el modo Día vs Rango en Estadísticas).</summary>
public class BoolToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Permite usar RadioButtons ligados directamente a un valor de enum.</summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null)
            return Enum.Parse(targetType, parameter.ToString()!);
        return Binding.DoNothing;
    }
}

/// <summary>Convierte una cantidad (int) y un máximo en un ancho de barra proporcional, para los gráficos simples.</summary>
public class CantidadAAnchoConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not int cantidad || values[1] is not int maximo || maximo <= 0)
            return 0.0;

        double anchoTotal = parameter != null ? System.Convert.ToDouble(parameter, culture) : 200.0;
        return anchoTotal * cantidad / maximo;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Igual que CantidadAAnchoConverter pero para montos en pesos (decimal), usado en los gráficos
/// de ingresos por categoría y tendencia de ventas por día.</summary>
public class MontoAAnchoConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not decimal monto || values[1] is not decimal maximo || maximo <= 0)
            return 0.0;

        double anchoTotal = parameter != null ? System.Convert.ToDouble(parameter, culture) : 200.0;
        return anchoTotal * (double)monto / (double)maximo;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>True (variación positiva) -> verde, False (variación negativa) -> rojo. Usado para colorear
/// el indicador de "vs. período anterior" en Estadísticas.</summary>
public class BoolAColorVariacionConverter : IValueConverter
{
    private static readonly SolidColorBrush Verde = new((Color)ColorConverter.ConvertFromString("#2FAE4E"));
    private static readonly SolidColorBrush Rojo = new((Color)ColorConverter.ConvertFromString("#E5352B"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? Verde : Rojo;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}