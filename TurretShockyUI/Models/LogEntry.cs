using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TurretShocky.Models
{
    public class LogEntry : ObservableObject
    {
        private string _message;
        private Color _color;
        
        public LogEntry(string message, Color color)
        {
            _message = message;
            _color = color;
        }

        public LogEntry() { }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }
        
        public Color Color
        {
            get { return _color; }
            set { SetProperty(ref _color, value); }
        }
    }
}
