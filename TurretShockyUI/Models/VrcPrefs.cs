using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace TurretShockyUI.Models
{
    public class VrcPrefs : ObservableObject
    {
        public VrcPrefs()
        {
            _funType = (FunType)Preferences.Get("funtype", 2);
            _minIntensity = Preferences.Get("minintensity", 0);
            _maxIntensity = Preferences.Get("maxintensity", 100);
            _cooldownTime = Preferences.Get("cooldown", 10f);
            _duration = Preferences.Get("duration", 1);
            _shockers = Preferences.Get("shockers", new ObservableCollection<Shocker>())!;
            _api = Preferences.Get("api", new ApiPrefs());
        }
        public VrcPrefs(bool fakeData)
        {
            _funType = FunType.Shock;
            _minIntensity = 10;
            _maxIntensity = 45;
            _cooldownTime = 12f;
            _duration = 1;
            _shockers = [
                new() { Name="Test Shocker1", Code="A2B66ABF", IsEnabled=true},
                new() { Name = "Test Shocker2", Code = "C6B22ABF", IsEnabled = true }
            ];
        }

        private FunType _funType;
        public FunType FunType
        {
            get => _funType;
            set
            {
                Preferences.Set("funtype", (int)value);
                SetProperty(ref _funType, value);
            }
        }

        private int _minIntensity;
        public int MinIntensity
        {
            get => _minIntensity;
            set
            {
                Preferences.Set("minintensity", value);
                SetProperty(ref _minIntensity, value);
            }
        }

        private int _maxIntensity;
        public int MaxIntensity
        {
            get => _maxIntensity;
            set
            {
                Preferences.Set("maxintensity", value);
                SetProperty(ref _maxIntensity, value);
            }
        }

        private float _cooldownTime;
        public float CooldownTime
        {
            get => _cooldownTime;
            set
            {
                Preferences.Set("cooldown", value);
                SetProperty(ref _cooldownTime, value);
            }
        }

        private int _duration;
        public int Duration
        {
            get => _duration;
            set
            {
                Preferences.Set("duration", value);
                SetProperty(ref _duration, value);
            }
        }

        private ObservableCollection<Shocker> _shockers;
        public ObservableCollection<Shocker> Shockers
        {
            get => _shockers;
            set
            {
                Preferences.Set("shockers", value);
                SetProperty(ref _shockers, value);
            }
        }

        private ApiPrefs _api;
        public ApiPrefs Api
        {
            get => _api;
            set
            {
                Preferences.Set("api", value);
                SetProperty(ref _api, value);
            }
        }
    }

    public enum FunType
    {
        Shock = 0,
        Vibration = 1,
        Idle = 2
    }
}
