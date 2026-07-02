using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using TurretShocky.Models;

namespace TurretShocky;

public partial class FileTriggerDirectoryDialog : Window
{
    private FileSettings? _fileSettings;
    public FileTriggerDirectoryDialog(FileSettings? fileSettings)
    {
        _fileSettings = fileSettings;

        InitializeComponent();

        if (_fileSettings != null)
        {
            DataContext = _fileSettings;
        }
        else
        {
            DataContext = new FileSettings();
        }
    }

    private void BrowseForDirectoryBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is FileSettings filesSettings)
        {
            Window? window = TopLevel.GetTopLevel(this) as Window;
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
        }
    }
}