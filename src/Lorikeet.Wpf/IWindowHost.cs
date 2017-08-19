using System.Windows;

namespace Lorikeet.Wpf
{
    public interface IWindowHost
    {
        void SetContentAndShowWindow(UIElement content);
        void CloseWindow();
    }
}
