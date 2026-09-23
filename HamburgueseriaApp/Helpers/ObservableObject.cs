using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HamburgueseriaApp.Helpers;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? nombrePropiedad = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return false;

        campo = valor;
        OnPropertyChanged(nombrePropiedad);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? nombrePropiedad = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
}
