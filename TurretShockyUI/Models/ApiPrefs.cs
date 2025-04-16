using CommunityToolkit.Mvvm.ComponentModel;

namespace TurretShockyUI.Models
{
    public class ApiPrefs : ObservableObject
    {
        public ApiPrefs()
        {
            _apikey = string.Empty;
            _username = string.Empty;
        }

        private string _apikey;
        public string ApiKey
        {
            get => _apikey;
            set
            {
                SetProperty(ref _apikey, value);
            }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
            }
        }
    }
}