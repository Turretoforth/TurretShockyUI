using CommunityToolkit.Mvvm.ComponentModel;

namespace TurretShockyUI.Models
{
    public class ShockTrigger : ObservableObject
    {
        public ShockTrigger()
        {
            _id = 0;
            _triggerText = string.Empty;
            _triggerMode = TriggerMode.Contains;
        }

        private uint _id;
        public uint Id
        {
            get => _id;
            set
            {
                SetProperty(ref _id, value);
            }
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
