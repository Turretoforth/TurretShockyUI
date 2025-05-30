using Avalonia.Controls;
using Avalonia.Interactivity;
using TurretShocky.Models;

namespace TurretShocky;

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
        ApiSettings? prefs = (DataContext as ApiSettings);
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
            ApiPrefs = new ApiSettings
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