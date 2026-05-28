using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace TurretShocky.Models
{
    public class Shocker : ObservableObject
    {
        public Shocker()
        {
            _uid = Guid.NewGuid();
            _name = string.Empty;
            _code = string.Empty;
            _type = ShockerType.PiShock;
            _isEnabled = false;
            _overrides = null;
        }

        private Guid _uid;
        public Guid Uid
        {
            get => _uid;
            set
            {
                SetProperty(ref _uid, value);
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                SetProperty(ref _name, value);
            }
        }

        private string _code;
        public string Code
        {
            get => _code;
            set
            {
                SetProperty(ref _code, value);
            }
        }

        private ShockerType _type;
        public ShockerType Type
        {
            get => _type;
            set
            {
                SetProperty(ref _type, value);
            }
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

        private ObservableCollection<ShockerOverride>? _overrides;
        public ObservableCollection<ShockerOverride>? Overrides
        {
            get => _overrides;
            set => SetProperty(ref _overrides, value);
        }
    }

    public enum ShockerType
    {
        PiShock,
        OpenShock,
    }
}
