using CommunityToolkit.Mvvm.ComponentModel;

namespace TurretShocky.Models
{
    public class ApiSettings : ObservableObject
    {
        public ApiSettings()
        {
            _apikey = string.Empty;
            _username = string.Empty;
            _openshockapikey = string.Empty;
            _openshockusername = string.Empty;
            _openshockbaseapi = "https://api.openshock.app"; // Default value, can be overridden
        }

        // PiShock API settings, do not change field names for compatibility with old versions
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

        // OpenShock API settings
        private string _openshockapikey;
        public string OpenShockApiToken
        {
            get => _openshockapikey;
            set
            {
                SetProperty(ref _openshockapikey, value);
            }
        }

        private string _openshockusername;
        public string OpenShockUsername
        {
            get => _openshockusername;
            set
            {
                SetProperty(ref _openshockusername, value);
            }
        }

        private string _openshockbaseapi;
        public string OpenShockBaseApi
        {
            get => _openshockbaseapi;
            set
            {
                SetProperty(ref _openshockbaseapi, value);
            }
        }
    }
}