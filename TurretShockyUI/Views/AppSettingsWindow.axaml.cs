using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.Linq;
using TurretShockyUI.Models;

namespace TurretShockyUI;

public partial class AppSettingsWindow : Window
{
    public AppSettingsWindow()
    {
        InitializeComponent();
    }

    override protected void OnDataContextEndUpdate()
    {
        FillInitialValues();
        base.OnDataContextEndUpdate();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!e.IsProgrammatic)
        {
            Close(new AppSettingsWindowResult
            {
                WatchFiles = WatchFiles.IsChecked ?? false,
                FilesSettings = (DataContext as AppSettings)?.FilesSettings.ToList() ?? []
            });
            e.Cancel = true; // Prevents default closing behavior
        }

        base.OnClosing(e);
    }

    private void FillInitialValues()
    {
        AppSettings? settings = (DataContext as AppSettings);
        if (settings != null)
        {
            WatchFiles.IsChecked = settings.WatchFiles;
        }
        else
        {
            WatchFiles.IsChecked = false;
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

    private void AddDirectoryBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is AppSettings appSettings && appSettings.FilesSettings is ObservableCollection<FileSettings> collection)
        {
            FileSettings newDirectory = new()
            {
                IsEnabled = false,
                DirectoryPath = string.Empty,
                FilePattern = string.Empty,
                ShockTriggers = []
            };
            collection.Add(newDirectory);
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
            }
        }
    }

    private void BrowseForDirectoryBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is FileSettings filesSettings)
        {
            StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
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
}