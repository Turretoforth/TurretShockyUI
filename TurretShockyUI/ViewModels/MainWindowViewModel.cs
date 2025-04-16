using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using TurretShockyUI.Models;

namespace TurretShockyUI.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<LogEntry> _logEntries = [];
        private VrcPrefs _prefs = new();
        private bool _isOscButtonEnabled = true;
        private uint _nbShocks = 0;
        private uint _nbtouches = 0;
        private uint _timesTriggered = 0;
        private uint _maxIntensity = 0;

        public ObservableCollection<LogEntry> LogEntries
        {
            get { return _logEntries; }
            set { SetProperty(ref _logEntries, value); }
        }

        public VrcPrefs Prefs
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

        public MainWindowViewModel()
        {
            LogEntries = [];
            Prefs = new VrcPrefs();
            IsOscButtonEnabled = true;
            NbShocks = 0;
            NbTouches = 0;
            TimesTriggered = 0;
            MaxIntensity = 0;
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
