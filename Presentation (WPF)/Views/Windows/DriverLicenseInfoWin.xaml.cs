using Application.DTOs;
using Application.Interfaces;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Presentation.Views.Windows
{
    public partial class DriverLicenseInfoWin : Window, INotifyPropertyChanged
    {
        private readonly int _localDrivingLicenseApplicationId;
        private readonly ILicenseService _licenseService;
        private readonly ILocalDrivingLicenseApplicationService _lDLAppService;

        private DriverLicenseInfoDto? _licenseData;
        public DriverLicenseInfoDto? LicenseData
        {
            get => _licenseData;
            set
            {
                _licenseData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsActiveStatus));
                OnPropertyChanged(nameof(IsDetainedStatus));
            }
        }

        private BitmapImage? _driverPhoto;
        public BitmapImage? DriverPhoto
        {
            get => _driverPhoto;
            set
            {
                _driverPhoto = value;
                OnPropertyChanged();
            }
        }

        public string IsActiveStatus =>
            LicenseData?.IsActive == true ? "Yes" : "No";

        public string IsDetainedStatus =>
            LicenseData?.IsDetained == true ? "Yes" : "No";

        public ICommand CloseCommand { get; }

        public DriverLicenseInfoWin(int localDrivingLicenseApplicationId)
        {
            InitializeComponent();

            _localDrivingLicenseApplicationId = localDrivingLicenseApplicationId;

            _licenseService = App.ServiceProvider.GetRequiredService<ILicenseService>();
            _lDLAppService = App.ServiceProvider.GetRequiredService<ILocalDrivingLicenseApplicationService>();

            CloseCommand = new RelayCommand(_ => Close());

            DataContext = this;

            Loaded += DriverLicenseInfoWin_Loaded;
        }

        private async void DriverLicenseInfoWin_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var applicationId = await _lDLAppService.GetApplicationIdByLocalIdAsync(_localDrivingLicenseApplicationId);

                if (applicationId == null)
                    return;

                //LicenseData = await _licenseService.GetByApplicationIdAsync((int)applicationId);

                if (LicenseData == null)
                    return;

                if (!string.IsNullOrWhiteSpace(LicenseData.ImagePath) &&
                    File.Exists(LicenseData.ImagePath))
                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(
                        LicenseData.ImagePath,
                        UriKind.Absolute);

                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    bitmap.Freeze();

                    DriverPhoto = bitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading license information:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand(Action<object?> execute) : ICommand
    {
        private readonly Action<object?> _execute = execute;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged;
    }
}