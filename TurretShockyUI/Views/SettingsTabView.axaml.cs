using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using TurretShocky.Models;
using TurretShocky.ViewModels;

namespace TurretShocky.Views;

public partial class SettingsTabView : UserControl
{
    public SettingsTabView()
    {
        InitializeComponent();
    }

    private void AddDirectoryBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is MainWindowViewModel vm && vm.Prefs.App.FilesSettings is ObservableCollection<FileSettings> collection)
        {
            FileSettings newDirectory = new()
            {
                IsEnabled = false,
                DirectoryPath = string.Empty,
                FilePattern = string.Empty,
                ShockTriggers = []
            };
            collection.Add(newDirectory);
            SaveParameters();
        }
    }

    private void RemoveDirectoryBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is FileSettings filesSettings)
        {
            ItemsControl? parent = button.FindAncestorOfType<ItemsControl>();
            if (parent != null && parent.ItemsSource is ObservableCollection<FileSettings> collection)
            {
                collection.Remove(filesSettings);
                SaveParameters();
            }
        }
    }

    private void EditDirectoryBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is FileSettings filesSettings)
        {
            Window? window = TopLevel.GetTopLevel(this) as Window;
            // Open a dialog to edit the directory settings
            FileTriggerDirectoryDialog dialog = new(filesSettings);
            dialog.ShowDialog(window!).ContinueWith(
                d => SaveParameters() // Trigger save to not lose changes
            );
        }
    }

    private void SaveParameters()
    {
        Dispatcher.Invoke(() =>
        {
            // Save the preferences (This is needed to ensure the changes are applied, else it's a coin toss if it's made in time)
            AppSettings appSettings = (DataContext as MainWindowViewModel)!.Prefs.App;
            (DataContext as MainWindowViewModel)!.Prefs.App = appSettings;
        });
    }

    private void HandlePropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (DataContext != null)
            SaveParameters();
    }

}