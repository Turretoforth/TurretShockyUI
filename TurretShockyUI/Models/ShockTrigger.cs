using CommunityToolkit.Mvvm.ComponentModel;

namespace TurretShockyUI.Models
{
    public class ShockTrigger : ObservableObject
    {
        public ShockTrigger()
        {
            _triggerText = string.Empty;
            _triggerMode = TriggerMode.Contains;
        }

        private string _triggerText;
        public string TriggerText
        {
            get => _triggerText;
            set
            {
                SetProperty(ref _triggerText, value);
            }
        }

        private TriggerMode _triggerMode;
        public TriggerMode TriggerMode
        {
            get => _triggerMode;
            set
            {
                SetProperty(ref _triggerMode, value);
            }
        }
    }

    public enum TriggerMode
    {
        Contains,
        StartsWith,
        EndsWith,
        Regex
    }
}
