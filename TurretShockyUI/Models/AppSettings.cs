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

        private ObservableCollection<FilesSettings> _filesSettings = [];
        public ObservableCollection<FilesSettings> FilesSettings
        {
            get => _filesSettings;
            set
            {
                SetProperty(ref _filesSettings, value);
            }
        }
    }
}
