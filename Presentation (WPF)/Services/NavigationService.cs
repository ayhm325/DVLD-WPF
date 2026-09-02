using System;
using System.Windows.Controls;

namespace Presentation.Services
{
    public sealed class NavigationService : INavigationService
    {
        private readonly Frame _frame;

        public NavigationService(Frame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public void Navigate(Page page)
        {
            if (page is null)
                throw new ArgumentNullException(nameof(page));

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