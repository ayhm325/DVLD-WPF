using System.Windows.Controls;

namespace Presentation.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Frame _frame;

        public NavigationService(Frame frame)
        {
            _frame = frame;
        }

        public void Navigate(Page page)
        {
            _frame.Navigate(page);
        }

        public void GoBack()
        {
            if (_frame.CanGoBack)
                _frame.GoBack();
        }

        public void GoForward()
        {
            if (_frame.CanGoForward)
                _frame.GoForward();
        }
    }
}