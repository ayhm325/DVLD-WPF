

using System.Windows.Controls;

namespace Presentation.Services
{
    public interface INavigationService
    {
        void Navigate(Page page);
        void GoBack();
        void GoForward();
    }
}