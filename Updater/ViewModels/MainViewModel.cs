namespace Updater.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        _statusMessage = "Waiting for TurretShocky to close...";
        _warningMessage = "Please wait while TurretShocky is updating.";
    }

    private string _warningMessage;
    public string WarningMessage
    {
        get { return _warningMessage; }
        set { SetProperty(ref _warningMessage, value); }
    }

    private string _statusMessage;
    public string UpdateStatusMessage
    {
        get { return _statusMessage; }
        set { SetProperty(ref _statusMessage, value); }
    }
}
