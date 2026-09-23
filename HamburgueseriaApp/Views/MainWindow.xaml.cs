using System.Windows;
using HamburgueseriaApp.ViewModels;

namespace HamburgueseriaApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
