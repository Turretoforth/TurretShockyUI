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
        if (prefs != null)
        {
            PiShockApiKey.Text = prefs.ApiKey;
            PiShockUsername.Text = prefs.Username;
            OpenShockApiToken.Text = prefs.OpenShockApiToken;
            OpenShockBaseApi.Text = prefs.OpenShockBaseApi;
        }
        else
        {
            PiShockApiKey.Text = string.Empty;
            PiShockUsername.Text = string.Empty;
            OpenShockApiToken.Text = string.Empty;
            OpenShockBaseApi.Text = "https://api.openshock.app"; // Default value, can be overridden
        }
    }

    public void ResultSave(object? sender, RoutedEventArgs e)
    {
        bool hasFilledOpenShock = !string.IsNullOrWhiteSpace(OpenShockApiToken.Text) && !string.IsNullOrWhiteSpace(OpenShockBaseApi.Text);
        bool hasFilledPiShock = !string.IsNullOrWhiteSpace(PiShockApiKey.Text) && !string.IsNullOrWhiteSpace(PiShockUsername.Text);
        if (!hasFilledOpenShock && !hasFilledPiShock)
        {
            // Show a simple error message
            ErrorDialog errorDialog = new("Api Key and Username needs to be filled for at least one of the APIs");
            errorDialog.ShowDialog(this);
            return;
        }

        Close(new ApiConfigWindowResult
        {
            ShouldSave = true,
            ApiPrefs = new ApiSettings
            {
                ApiKey = PiShockApiKey.Text ?? string.Empty,
                Username = PiShockUsername.Text ?? string.Empty,
                OpenShockApiToken = OpenShockApiToken.Text ?? string.Empty,
                OpenShockBaseApi = OpenShockBaseApi.Text ?? "https://api.openshock.app" // Default value, can be overridden
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