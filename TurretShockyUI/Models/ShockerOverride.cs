using CommunityToolkit.Mvvm.ComponentModel;

namespace TurretShocky.Models
{
    public class ShockerOverride : ObservableObject
    {
        public ShockerOverride()
        {
            _overrideType = ShockerOverrideType.Duration;
            _overrideMode = ShockerOverrideMode.Exactly;
            _overrideValue = 0;
        }

        private ShockerOverrideType _overrideType;
        public ShockerOverrideType OverrideType
        {
            get => _overrideType;
            set => SetProperty(ref _overrideType, value);
        }

        private ShockerOverrideMode _overrideMode;
        public ShockerOverrideMode OverrideMode
        {
            get => _overrideMode;
            set => SetProperty(ref _overrideMode, value);
        }

        private int _overrideValue;
        public int OverrideValue
        {
            get => _overrideValue;
            set => SetProperty(ref _overrideValue, value);
        }
    }

    public enum ShockerOverrideType
    {
        Duration = 0,
        Intensity = 1,
    }

    public enum ShockerOverrideMode
    {
        Exactly = 0,
        Maximum = 1,
        Minimum = 2
    }
}