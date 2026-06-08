


using System.Windows.Controls;

namespace Presentation.Views
{
    public partial class UserPage : Page
    {
        private UsersViewModel? _viewModel => DataContext as UsersViewModel;

        public UserPage(UsersViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.IsVisibleChanged += async (s, e) => {
                if ((bool)e.NewValue && _viewModel != null) await _viewModel.LoadUsersAsync();
            };
        }
    }
}