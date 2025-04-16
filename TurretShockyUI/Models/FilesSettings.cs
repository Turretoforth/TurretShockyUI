using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace TurretShockyUI.Models
{
    public class FilesSettings : ObservableObject
    {
        public FilesSettings()
        {
            _isEnabled = false;
            _directoryPath = string.Empty;
            _filePattern = string.Empty;
            _shockTriggers = [];
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                SetProperty(ref _isEnabled, value);
            }
        }

        private string _directoryPath;
        public string DirectoryPath
        {
            get => _directoryPath;
            set
            {
                SetProperty(ref _directoryPath, value);
            }
        }

        private string _filePattern;
        public string FilePattern
        {
            get => _filePattern;
            set
            {
                SetProperty(ref _filePattern, value);
            }
        }

        private ObservableCollection<ShockTrigger> _shockTriggers;
        public ObservableCollection<ShockTrigger> ShockTriggers
        {
            get => _shockTriggers;
            set
            {
                SetProperty(ref _shockTriggers, value);
            }
        }
    }
}
