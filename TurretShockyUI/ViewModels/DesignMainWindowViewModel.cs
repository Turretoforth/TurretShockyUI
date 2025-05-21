using TurretShocky.Models;

namespace TurretShocky.ViewModels
{
    public partial class DesignMainWindowViewModel : MainWindowViewModel
    {
        public DesignMainWindowViewModel()
        {
            LogEntries = [];
            Prefs = new ShockyPrefs(true);
            IsOscButtonEnabled = true;
            NbShocks = 0;
            NbTouches = 0;
            TimesTriggered = 0;
            MaxIntensity = 0;
        }
    }
}
