using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using TurretShockyUI.Models;

namespace TurretShockyUI;

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
            this.FindControl<TextBox>("Name").Text = _selectedShocker.Name;
            this.FindControl<TextBox>("Code").Text = _selectedShocker.Code;
        }
        else
        {
            this.FindControl<TextBox>("Name").Text = string.Empty;
            this.FindControl<TextBox>("Code").Text = string.Empty;
        }
    }

    public void ResultSave(object? sender, RoutedEventArgs e)
    {
        Close(new ShockerConfigWindowResult
        {
            ShouldSave = true,
            Shocker = new Shocker
            {
                Uid = _selectedShocker?.Uid ?? Guid.NewGuid(),
                Name = this.FindControl<TextBox>("Name").Text,
                Code = this.FindControl<TextBox>("Code").Text
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