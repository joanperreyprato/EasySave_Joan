using System.IO;
using System.Windows;
using System.Windows.Input;
using EasySave.Core.Models;
using EasySave.WPF.ViewModels;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

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
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    private void LogFormatCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Guard: ViewModel may not be set yet during XAML initialization
        if (_viewModel is null) return;

        if (sender is WpfComboBox combo && combo.SelectedItem is WpfComboBoxItem item)
        {
            string format = item.Content?.ToString() ?? "JSON";
            _viewModel.SelectedLogFormat = format;
        }
    }
}
