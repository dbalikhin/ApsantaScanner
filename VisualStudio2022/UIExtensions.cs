using System.Windows;
using System.Windows.Threading;

namespace VisualStudio2022
{
    internal static class UIExtensions
    {
        private static readonly Action EmptyDelegate = delegate { };
        internal static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }        
    }
}
