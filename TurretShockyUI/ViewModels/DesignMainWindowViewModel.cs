using TurretShocky.Models;

namespace TurretShocky.ViewModels
{
    public partial class DesignMainWindowViewModel : MainWindowViewModel
    {
        public DesignMainWindowViewModel()
        {
            LogEntries = [];
            Prefs = new ShockyPrefs(true);
            IsOscEnabled = false;
            NbShocks = 0;
            NbTouches = 0;
            TimesTriggered = 0;
            MaxIntensity = 0;
            HasUpdateAvailable = true;
            UpdateVersion = "1.3.2.4123";
            CurrentVersion = "1.3.1.1234"; // Design-time version, can be set to any value
        }
    }
}
