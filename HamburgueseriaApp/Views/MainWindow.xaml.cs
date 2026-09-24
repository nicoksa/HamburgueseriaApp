using Wpf.Ui.Controls;
using HamburgueseriaApp.ViewModels;

namespace HamburgueseriaApp.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}