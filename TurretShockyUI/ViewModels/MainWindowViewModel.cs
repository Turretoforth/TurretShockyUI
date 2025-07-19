using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using TurretShocky.Models;

namespace TurretShocky.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<LogEntry> _logEntries = [];
        private ShockyPrefs _prefs = new();
        private bool _isOscButtonEnabled = true;
        private uint _nbShocks = 0;
        private uint _nbtouches = 0;
        private uint _timesTriggered = 0;
        private uint _maxIntensity = 0;
        private bool _hasUpdateAvailable = false;
        private string _updateVersion = "0.0.0";
        private string? _currentVersion = Utils.GetCurrentVersion();

        public ObservableCollection<LogEntry> LogEntries
        {
            get { return _logEntries; }
            set { SetProperty(ref _logEntries, value); }
        }

        public ShockyPrefs Prefs
        {
            get { return _prefs; }
            set { SetProperty(ref _prefs, value); }
        }

        public bool IsOscButtonEnabled
        {
            get { return _isOscButtonEnabled; }
            set { SetProperty(ref _isOscButtonEnabled, value); }
        }

        public uint NbShocks
        {
            get { return _nbShocks; }
            set { SetProperty(ref _nbShocks, value); }
        }

        public uint NbTouches
        {
            get { return _nbtouches; }
            set { SetProperty(ref _nbtouches, value); }
        }

        public uint TimesTriggered
        {
            get { return _timesTriggered; }
            set { SetProperty(ref _timesTriggered, value); }
        }

        public uint MaxIntensity
        {
            get { return _maxIntensity; }
            set { SetProperty(ref _maxIntensity, value); }
        }

        public bool HasUpdateAvailable
        {
            get { return _hasUpdateAvailable; }
            set { SetProperty(ref _hasUpdateAvailable, value); }
        }

        public string UpdateVersion
        {
            get { return _updateVersion; }
            set { SetProperty(ref _updateVersion, value); }
        }

        public string? CurrentVersion
        {
            get { return _currentVersion; }
            set { SetProperty(ref _currentVersion, value); }
        }

        public bool RouletteMode
        {
            get { return Prefs.RouletteMode; }
            set
            {
                if (Prefs.RouletteMode != value)
                {
                    Prefs.RouletteMode = value;
                }
            }
        }

        public MainWindowViewModel()
        {
            LogEntries = [];
            Prefs = new ShockyPrefs();
            IsOscButtonEnabled = true;
            NbShocks = 0;
            NbTouches = 0;
            TimesTriggered = 0;
            MaxIntensity = 0;
            HasUpdateAvailable = false;
            UpdateVersion = "0.0.0";
            CurrentVersion = Utils.GetCurrentVersion();
        }

        public void AddLog(string message, Color color)
        {
            LogEntries.Add(new LogEntry($"[{DateTime.Now:HH:mm:ss}] {message}", color));
            if (LogEntries.Count > 10)
            {
                LogEntries.RemoveAt(0);
            }
        }
    }
}
