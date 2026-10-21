using System.Windows;
using System.Windows.Input;
using EasySave.WPF.ViewModels;

namespace EasySave.WPF.Views;

/// <summary>
/// Code-behind for MainWindow. Contains only UI event handlers — zero business logic.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    /// <summary>Initializes the window with its ViewModel.</summary>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void NewJobButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddJobWindow(_viewModel) { Owner = this };
        dialog.ShowDialog();
    }

    private void JobsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
