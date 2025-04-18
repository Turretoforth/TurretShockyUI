using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace TurretShockyUI.Models
{
    public class AppSettings : ObservableObject
    {
        public AppSettings() { }

        private bool _watchFiles = false;
        public bool WatchFiles
        {
            get => _watchFiles;
            set
            {
                SetProperty(ref _watchFiles, value);
            }
        }

        private CooldownBehaviour _cooldownBehaviour = CooldownBehaviour.Ignore;
        public CooldownBehaviour CooldownBehaviour
        {
            get => _cooldownBehaviour;
            set
            {
                SetProperty(ref _cooldownBehaviour, value);
            }
        }

        private ObservableCollection<FileSettings> _filesSettings = [];
        public ObservableCollection<FileSettings> FilesSettings
        {
            get => _filesSettings;
            set
            {
                SetProperty(ref _filesSettings, value);
            }
        }
    }

    public enum CooldownBehaviour
    {
        Ignore,
        Queue
    }

    public class DesignAppSettings : AppSettings
    {
        public DesignAppSettings()
        {
            WatchFiles = true;
            CooldownBehaviour = CooldownBehaviour.Ignore;
            FilesSettings =
            [
                new() {
                    IsEnabled = true,
                    DirectoryPath = "C:\\MyDirectory",
                    FilePattern = "output*.txt",
                    ShockTriggers =
                    [
                        new() { Id = 0, TriggerMode = TriggerMode.Contains, TriggerText = "You died" },
                        new() { Id = 1, TriggerMode = TriggerMode.Regex, TriggerText = "Hit - [0-9]*.*" }
                    ]
                }
            ];
        }
    }
}
