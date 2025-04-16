using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Data;
using TurretShockyUI.Models;

namespace TurretShockyUI;

public partial class ApiConfigWindow : Window
{
    public ApiConfigWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextEndUpdate()
    {
        FillInitialValues();
        base.OnDataContextEndUpdate();
    }

    private void FillInitialValues()
    {
        ApiPrefs? prefs = (DataContext as ApiPrefs);
        if(prefs != null)
        {
            this.FindControl<TextBox>("ApiKey").Text = prefs.ApiKey;
            this.FindControl<TextBox>("Username").Text = prefs.Username;
        }
        else
        {
            this.FindControl<TextBox>("ApiKey").Text = string.Empty;
            this.FindControl<TextBox>("Username").Text = string.Empty;
        }
    }

    public void ResultSave(object? sender, RoutedEventArgs e)
    {
        Close(new ApiConfigWindowResult
        {
            ShouldSave = true,
            ApiPrefs = new ApiPrefs
            {
                ApiKey = this.FindControl<TextBox>("ApiKey").Text,
                Username = this.FindControl<TextBox>("Username").Text
            }
        });
    }

    public void ResultCancel(object? sender, RoutedEventArgs e)
    {
        Close(new ApiConfigWindowResult
        {
            ShouldSave = false,
            ApiPrefs = null
        });
    }
}