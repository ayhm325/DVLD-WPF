using Application.DTOs;
using Application.Interfaces;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Presentation.Views.Windows
{
    public partial class DriverLicenseInfoWin : Window, INotifyPropertyChanged
    {
        private readonly int _localDrivingLicenseApplicationId;
        private readonly ILicenseService _licenseService;

        // الخاصية التي سترتبط بها الـ XAML
        private DriverLicenseInfoDto? _licenseData;
        public DriverLicenseInfoDto? LicenseData
        {
            get => _licenseData;
            set { _licenseData = value; OnPropertyChanged(); }
        }

        public ICommand CloseCommand { get; }

        public DriverLicenseInfoWin(int lDLAppId)
        {
            InitializeComponent();
            _localDrivingLicenseApplicationId = lDLAppId;
            _licenseService = App.ServiceProvider.GetRequiredService<ILicenseService>();

            // ضبط الـ DataContext لهذا الكود
            this.DataContext = this;
            CloseCommand = new RelayCommand(_ => this.Close());

            this.Loaded += DriverLicenseInfoWin_Loaded;
        }

        private async void DriverLicenseInfoWin_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var fullLicenseDto = await _licenseService.GetDetails(_localDrivingLicenseApplicationId);
                if (fullLicenseDto != null)
                {
                    LicenseData = fullLicenseDto; // سيتم تحديث الـ UI تلقائياً
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
            => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter)
            => _execute(parameter);

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
       
    }
}