using Avalonia.Controls;
using Avalonia.Layout;

namespace TurretShocky.Models
{
    public class ErrorDialog : Window
    {
        public ErrorDialog(string message)
        {
            Title = "Error";
            Button closeBtn = new()
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            };
            closeBtn.Click += (s, e) => Close();
            Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    closeBtn
                }
            };
            Width = 300;
            Height = 180;
        }
    }
}
