using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using TurretShocky.Models;

namespace TurretShocky;

public partial class ShockerConfigWindow : Window
{
    private Shocker? _selectedShocker;
    private bool _isNew;

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
}