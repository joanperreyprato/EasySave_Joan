using System.IO;
using System.Windows;
using EasySave.Core.Models;
using EasySave.WPF.ViewModels;

namespace EasySave.WPF.Views;

/// <summary>
/// Code-behind for the New Job dialog window.
/// </summary>
public partial class AddJobWindow : Window
{
    private readonly MainViewModel _viewModel;

    /// <summary>Initializes the dialog.</summary>
    public AddJobWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
        => SourceBox.Text = BrowseFolder(SourceBox.Text);

    private void BrowseTarget_Click(object sender, RoutedEventArgs e)
        => TargetBox.Text = BrowseFolder(TargetBox.Text);

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var type = FullRadio.IsChecked == true ? BackupType.Full : BackupType.Differential;
        var result = _viewModel.CreateJob(
            NameBox.Text.Trim(),
            SourceBox.Text.Trim(),
            TargetBox.Text.Trim(),
            type);

        if (result == BackupCreationResult.Success)
        {
            DialogResult = true;
            return;
        }

        ErrorText.Text = result switch
        {
            BackupCreationResult.InvalidName    => "⚠ Invalid name. Use 1–100 alphanumeric characters, spaces, dashes or underscores.",
            BackupCreationResult.SourceNotFound => "⚠ Source directory not found.",
            BackupCreationResult.TargetNotFound => "⚠ Target parent directory not found.",
            BackupCreationResult.DuplicateName  => "⚠ A job with this name already exists.",
            _                                   => "⚠ An error occurred."
        };
        ErrorText.Visibility = Visibility.Visible;
    }

    private static string BrowseFolder(string initialPath)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select a directory",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(initialPath) ? initialPath : string.Empty
        };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.SelectedPath
            : initialPath;
    }
}
