using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUser;
        private readonly IServiceProvider _serviceProvider;


        public LoginViewModel(
            IUserService userService,
            ICurrentUserService currentUser,
            IServiceProvider serviceProvider)
        {
            _userService = userService;
            _currentUser = currentUser;
            _serviceProvider = serviceProvider;
        }

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [RelayCommand]
        private async Task LoginAsync()
        {
            var user = await _userService.LoginAsync(
                Username,
                Password);

            if (user == null)
            {
                MessageBox.Show("Invalid username or password.");
                return;
            }

            _currentUser.UserId = user.UserId;
            _currentUser.Username = user.UserName;
            _currentUser.FullName = user.FullName;

            var mainWindow =
                _serviceProvider.GetRequiredService<MainWindow>();

            mainWindow.Show();

            System.Windows.Application.Current.Windows
                .OfType<LoginWindow>()
                .FirstOrDefault()
                ?.Close();
        }        
    }
}
