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
    public partial class DriverInterNationalLicenseInfoWin : Window, INotifyPropertyChanged
    {
        private readonly int _internationalLicenseApplicationId;
        private readonly IInternationalService _internationalLicenseService;

        // الخاصية التي سترتبط بها الـ XAML
        private InternationalDto? _licenseData;
        public InternationalDto? LicenseData
        {
            get => _licenseData;
            set { _licenseData = value; OnPropertyChanged(); }
        }

        public ICommand CloseCommand { get; }

        public DriverInterNationalLicenseInfoWin(int internationalLicenseApplicationId)
        {
            InitializeComponent();
            _internationalLicenseApplicationId = internationalLicenseApplicationId;
            _internationalLicenseService = App.ServiceProvider.GetRequiredService<IInternationalService>();

            // ضبط الـ DataContext لهذا الكود
            this.DataContext = this;
            CloseCommand = new RelayCommand(_ => this.Close());

            this.Loaded += DriverInterNationalLicenseInfoWin_Loaded;
        }

        private async void DriverInterNationalLicenseInfoWin_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var fullLicenseDto = await _internationalLicenseService.GetByIdAsync(_internationalLicenseApplicationId);
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
}