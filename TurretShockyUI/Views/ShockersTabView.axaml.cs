using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Linq;
using TurretShocky.Models;
using TurretShocky.Services;
using TurretShocky.ViewModels;

namespace TurretShocky.Views;

public partial class ShockersTabView : UserControl
{
    public ShockersTabView()
    {
        InitializeComponent();
    }

    private void AddLog(string message, Color color)
    {
        Dispatcher.Invoke(() =>
        {
            (DataContext as MainWindowViewModel)?.AddLog(message, color);
        }, DispatcherPriority.MaxValue);
    }

    private void OnEditOrCreateShockerBtnClick(object? sender, RoutedEventArgs e)
    {
        // Check if the button clicked is the "New" button
        bool isNew = (sender as Button)!.Name == "NewShockerBtn";
        Shocker? selectedShocker = null;
        if (!isNew)
        {
            selectedShocker = (DataContext as MainWindowViewModel)!.Prefs.Shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as Button)!.Name);
        }

        // Open the Shocker configuration window
        var shockerConfigWindow = new ShockerConfigWindow(isNew, selectedShocker)
        {
            DataContext = (DataContext as MainWindowViewModel)!.Prefs.Shockers
        };
        var window = TopLevel.GetTopLevel(this) as Window;
        shockerConfigWindow.ShowDialog<ShockerConfigWindowResult>(window!)
            .ContinueWith(t =>
            {
                // Check if there is something to save
                if (t.Result != null && t.Result.ShouldSave && t.Result.Shocker != null)
                {
                    // Save the shocker
                    Dispatcher.Invoke(() =>
                    {
                        System.Collections.ObjectModel.ObservableCollection<Shocker> shockers = (DataContext as MainWindowViewModel)!.Prefs.Shockers;
                        if (t.Result.IsNew)
                        {
                            shockers.Add(t.Result.Shocker);
                        }
                        else if (selectedShocker != null)
                        {
                            shockers.Remove(selectedShocker);
                            shockers.Add(t.Result.Shocker);
                        }
                        (DataContext as MainWindowViewModel)!.Prefs.Shockers = shockers;
                    });
                }
            }
        );
    }

    private void OnConfigureApiBtnClick(object? sender, RoutedEventArgs e)
    {
        // Open the Api configuration window
        var apiConfigWindow = new ApiConfigWindow
        {
            DataContext = (DataContext as MainWindowViewModel)!.Prefs.Api
        };
        var window = TopLevel.GetTopLevel(this) as Window;
        apiConfigWindow.ShowDialog<ApiConfigWindowResult>(window!)
            .ContinueWith(t =>
            {
                // Check if the user clicked the save button
                if (t.Result != null && t.Result.ShouldSave)
                {
                    // Save the preferences
                    Dispatcher.Invoke(() =>
                    {
                        (DataContext as MainWindowViewModel)!.Prefs.Api = t.Result.ApiPrefs ?? new();
                        // Reinitialize the OpenShockService with the new (potential) API settings
                        OpenShockService.Initialize(
                            (DataContext as MainWindowViewModel)!.Prefs.Api.OpenShockBaseApi,
                            (DataContext as MainWindowViewModel)!.Prefs.Api.OpenShockApiToken
                        );
                        // Reinitialize the PiShockService with the new (potential) API settings
                        PiShockService.Initialize(
                            (DataContext as MainWindowViewModel)!.Prefs.Api.ApiKey,
                            (DataContext as MainWindowViewModel)!.Prefs.Api.Username
                        );
                    });
                }
            }
        );
    }

    private void DeleteShockerBtn(object? sender, RoutedEventArgs e)
    {
        // Get the name of the shocker to delete
        var shockers = (DataContext as MainWindowViewModel)!.Prefs.Shockers;
        Shocker? selectedShocker = shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as Button)!.Name);
        if (selectedShocker != null)
        {
            // Remove the shocker from the list
            shockers.Remove(selectedShocker);
            // Update the DataContext
            (DataContext as MainWindowViewModel)!.Prefs.Shockers = shockers;
        }
    }

    private void ShockerEnableClick(object? sender, RoutedEventArgs e)
    {
        // Get the name of the shocker to enable/disable
        var shockers = (DataContext as MainWindowViewModel)!.Prefs.Shockers;
        Shocker? selectedShocker = shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as CheckBox)!.Name);
        if (selectedShocker != null)
        {
            // Toggle the enabled state of the shocker
            selectedShocker.IsEnabled = (sender as CheckBox)!.IsChecked ?? false;
            // Update the DataContext
            (DataContext as MainWindowViewModel)!.Prefs.Shockers = shockers;
        }
    }

    private void OnTestShockerBtnClick(object? sender, RoutedEventArgs e)
    {
        // Get the shocker to test
        Shocker? selectedShocker = (DataContext as MainWindowViewModel)!.Prefs.Shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as Button)!.Name);
        if (selectedShocker == null)
        {
            AddLog("Couldn't find shocker to test? (Report this)", Colors.Red);
            return;
        }
        AddLog($"Triggering a test [1 second - 80%] vibration on {selectedShocker.Name}", Colors.Yellow);

        try
        {
            if (selectedShocker.Type == ShockerType.PiShock)
            {
                PiShockService.DoPiShockOperations(FunType.Vibration, 1, 80, [selectedShocker.Code])
                    .ContinueWith(r =>
                    {
                        foreach (var shocker in r.Result)
                        {
                            if (!shocker.Value.Success)
                            {
                                AddLog($"Error triggering PiShock {shocker.Key}: {shocker.Value.Message}", Colors.Red);
                            }
                        }
                    });
            }
            else if (selectedShocker.Type == ShockerType.OpenShock)
            {
                OpenShockService.SendShockerCommand([selectedShocker.Code], FunType.Vibration, 80, 1000)
                    .ContinueWith(r =>
                    {
                        if (r.IsFaulted)
                        {
                            AddLog($"Error triggering OpenShock: {r.Exception?.Message}", Colors.Red);
                        }
                    });
            }
            else
            {
                AddLog($"Unknown shocker type: {selectedShocker.Type}", Colors.Red);
            }
        }
        catch (Exception ex)
        {
            AddLog($"Error testing shocker: {ex.Message}", Colors.Red);
        }
    }

    private void ShockerSelectClick(object? sender, RoutedEventArgs e)
    {
        // Get the name of the shocker to enable/disable
        var shockers = (DataContext as MainWindowViewModel)!.Prefs.Shockers;
        Shocker? selectedShocker = shockers.FirstOrDefault(s => s.Uid.ToString() == (sender as CheckBox)!.Name);
        if (selectedShocker != null)
        {
            // Toggle the selected state of the shocker
            selectedShocker.IsSelected = (sender as CheckBox)!.IsChecked ?? false;
            // Update the DataContext
            (DataContext as MainWindowViewModel)!.Prefs.Shockers = shockers;
        }
    }
}