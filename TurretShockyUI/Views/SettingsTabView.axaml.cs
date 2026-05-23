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

    private void RemoveTriggerBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ShockTrigger trigger)
        {
            ItemsControl? parent = button.FindAncestorOfType<ItemsControl>();
            if (parent != null && parent.ItemsSource is ObservableCollection<ShockTrigger> collection)
            {
                collection.Remove(trigger);

                // Update the IDs of the remaining triggers
                for (int i = 0; i < collection.Count; i++)
                {
                    collection[i].Id = (uint)i;
                }
                SaveParameters();
            }
        }
    }

    private void AddTriggerBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is FileSettings filesSettings && filesSettings.ShockTriggers is ObservableCollection<ShockTrigger> collection)
        {
            ShockTrigger newTrigger = new()
            {
                Id = (uint)collection.Count,
                TriggerText = string.Empty,
                TriggerMode = TriggerMode.Contains
            };
            collection.Add(newTrigger);
            SaveParameters();
        }
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

    private void BrowseForDirectoryBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is FileSettings filesSettings)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            window!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Directory",
                AllowMultiple = false
            }).ContinueWith(task =>
            {
                if (task.Result != null && task.Result.Count > 0)
                {
                    filesSettings.DirectoryPath = task.Result[0].TryGetLocalPath() ?? string.Empty;
                }
            });
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