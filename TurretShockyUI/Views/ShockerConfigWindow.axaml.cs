using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TurretShocky.Models;
using TurretShocky.Services;

namespace TurretShocky;

public partial class ShockerConfigWindow : Window
{
    private Shocker? _selectedShocker;
    private bool _isNew;
    private List<OpenShocker> _openShockers = [];

    public ShockerConfigWindow() : this(true, null) { }

    public ShockerConfigWindow(bool isNew, Shocker? selectedShocker)
    {
        _isNew = isNew;
        _selectedShocker = selectedShocker;

        InitializeComponent();

        if (_selectedShocker != null)
        {
            Name.Text = _selectedShocker.Name;
            Code.Text = _selectedShocker.Code;
            PiShockType.IsChecked = _selectedShocker.Type == ShockerType.PiShock;
            OpenShockType.IsChecked = _selectedShocker.Type == ShockerType.OpenShock;
        }
        else
        {
            Name.Text = string.Empty;
            Code.Text = string.Empty;
            PiShockType.IsChecked = true;
            OpenShockType.IsChecked = false;
        }
    }

    public void ResultSave(object? sender, RoutedEventArgs e)
    {
        ShockerType selectedType = ShockerType.PiShock; // Default to PiShock
        if (PiShockType.IsChecked == true)
        {
            selectedType = ShockerType.PiShock;
        }
        else if (OpenShockType.IsChecked == true)
        {
            selectedType = ShockerType.OpenShock;
        }

        if (string.IsNullOrEmpty(Name.Text) || string.IsNullOrEmpty(Code.Text))
        {
            // Show a simple error message
            var errorDialog = new ErrorDialog("Name and/or Code cannot be empty.");
            errorDialog.ShowDialog(this);

            return;
        }

        Close(new ShockerConfigWindowResult
        {
            ShouldSave = true,
            Shocker = new Shocker
            {
                Uid = _selectedShocker?.Uid ?? Guid.NewGuid(),
                Name = Name.Text ?? "?",
                Code = Code.Text ?? "?",
                Type = selectedType,
            },
            IsNew = _isNew
        });
    }

    public void ResultCancel(object? sender, RoutedEventArgs e)
    {
        Close(new ShockerConfigWindowResult
        {
            ShouldSave = false,
            Shocker = null,
            IsNew = _isNew
        });
    }

    private void PiShock_Checked(object? sender, RoutedEventArgs e)
    {
        OSSelectStack.IsVisible = false;
        OSShockerSelect.IsVisible = false;
        OSShockerSelect.Items.Clear();
    }

    private void OpenShock_Checked(object? sender, RoutedEventArgs e)
    {
        OSSelectStack.IsVisible = true;
        OSShockerSelect.IsVisible = true;
        try
        {
            OSShockerSelect.Items.Clear();
            OSShockerSelect.Items.Add(new ComboBoxItem
            {
                Content = "Loading...",
                Name = "default"
            });
            OSShockerSelect.SelectedIndex = 0; // Select the first item by default

            Task.Run(async () =>
            {
                try
                {
                    List<OpenShocker> openShockers = [];
                    List<OpenShocker> ownShockers = await OpenShockService.GetOwnShockers();
                    openShockers.AddRange(ownShockers);
                    List<OpenShocker> sharedShockers = await OpenShockService.GetSharedShockers();
                    openShockers.AddRange(sharedShockers);

                    _openShockers = openShockers; // Store the fetched shockers

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        OSShockerSelect.Items.Clear(); // Clear the loading item
                        OSShockerSelect.Items.Add(new ComboBoxItem
                        {
                            Content = openShockers.Count > 0 ? "Select a shocker" : "No shockers found",
                            Name = "default"
                        });
                        OSShockerSelect.SelectedIndex = 0; // Refresh the selection
                    }, DispatcherPriority.MaxValue);

                    foreach (OpenShocker shocker in ownShockers)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            OSShockerSelect.Items.Add(new ComboBoxItem
                            {
                                Content = $"{shocker.Name} ({(shocker.IsPaused ? "Paused" : "Active")})",
                                Name = shocker.Id.ToString()
                            });
                        }, DispatcherPriority.MaxValue);
                    }

                    foreach (OpenShocker shocker in sharedShockers)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            OSShockerSelect.Items.Add(new ComboBoxItem
                            {
                                Content = $"{shocker.Name} (Shared - {(shocker.IsPaused ? "Paused" : "Active")})",
                                Name = shocker.Id.ToString()
                            });
                        }, DispatcherPriority.MaxValue);
                    }
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ErrorDialog errorDialog = new($"An error occurred: {ex.Message}");
                        errorDialog.ShowDialog(this);
                    }, DispatcherPriority.MaxValue);
                }
            });
        }
        catch (Exception ex)
        {
            ErrorDialog errorDialog = new($"An error occurred: {ex.Message}");
            errorDialog.ShowDialog(this);
        }
    }

    private void OSShocker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (OSShockerSelect.SelectedItem is ComboBoxItem selectedItem && selectedItem.Name != null && selectedItem.Name != "default")
            {
                Guid selectedId = Guid.Parse(selectedItem.Name);
                OpenShocker? selectedShocker = _openShockers.Find(s => s.Id == selectedId);
                Code.Text = selectedShocker != default ? selectedShocker.Id.ToString() : string.Empty;
                Name.Text = selectedShocker != default ? selectedShocker.Name : string.Empty;
            }
            else
            {
                Code.Text = string.Empty;
                Name.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            ErrorDialog errorDialog = new($"An error occurred: {ex.Message}");
            errorDialog.ShowDialog(this);
        }
    }
}