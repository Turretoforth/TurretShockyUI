using Avalonia.Controls;
using TurretShocky.Models;
using TurretShocky.Services;

namespace TurretShocky;

public partial class UpdateChangelogWindow : Window
{
    private readonly UpdateService _updateService;

    public UpdateChangelogWindow() : this(new UpdateService("TurretShocky", "TurretShocky", "TurretShocky.zip")) { }
    public UpdateChangelogWindow(UpdateService updateService)
    {
        InitializeComponent();
        _updateService = updateService;
        ChangelogTitle.Text = _updateService.UpdateTitle;
        ChangelogHtmlPanel.Text = _updateService.UpdateHTMLChangelog;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!e.IsProgrammatic)
        {
            Close(new UpdateChangelogWindowResult
            {
                ShouldUpdate = false
            });
            e.Cancel = true; // Prevents default closing behavior
        }

        base.OnClosing(e);
    }

    public void UpdateBtn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(new UpdateChangelogWindowResult
        {
            ShouldUpdate = true
        });
    }
}