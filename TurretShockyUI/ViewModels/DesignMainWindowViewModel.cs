﻿using TurretShocky.Models;

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
            HasUpdateAvailable = true;
            UpdateVersion = "1.2.3.0";
            CurrentVersion = "1.0.0.0"; // Design-time version, can be set to any value
        }
    }
}
