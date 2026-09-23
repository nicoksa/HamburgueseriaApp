using System.Windows.Input;

namespace HamburgueseriaApp.Helpers;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _ejecutar;
    private readonly Predicate<object?>? _puedeEjecutar;

    public RelayCommand(Action<object?> ejecutar, Predicate<object?>? puedeEjecutar = null)
    {
        _ejecutar = ejecutar ?? throw new ArgumentNullException(nameof(ejecutar));
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parametro) => _puedeEjecutar?.Invoke(parametro) ?? true;

    public void Execute(object? parametro) => _ejecutar(parametro);
}
