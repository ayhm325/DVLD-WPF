using DVLD.Contracts.InternationalLicense;
using Presentation.Services.Api;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Presentation.Views.Windows
{
    public partial class DriverInterNationalLicenseInfoWin : Window, INotifyPropertyChanged
    {
        private readonly int _internationalLicenseId;
        private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;

        private InternationalLicenseResponse? _licenseData;

        public InternationalLicenseResponse? LicenseData
        {
            get => _licenseData;
            set
            {
                _licenseData = value;
                OnPropertyChanged();
            }
        }

        public ICommand CloseCommand { get; }

        public DriverInterNationalLicenseInfoWin(
            int internationalLicenseId,
            IInternationalLicensesApiClient internationalLicensesApiClient)
        {
            InitializeComponent();

            _internationalLicenseId = internationalLicenseId;

            _internationalLicensesApiClient =
                internationalLicensesApiClient
                ?? throw new ArgumentNullException(
                    nameof(internationalLicensesApiClient));

            DataContext = this;

            CloseCommand = new RelayCommand(_ => Close());

            Loaded += DriverInterNationalLicenseInfoWin_Loaded;
        }

        private async void DriverInterNationalLicenseInfoWin_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var result =
                    await _internationalLicensesApiClient
                        .GetByIdAsync(_internationalLicenseId);

                if (result.IsFailure)
                {
                    MessageBox.Show(
                        result.Error,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                if (result.Value is null)
                {
                    MessageBox.Show(
                        "International license was not found.",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                LicenseData = result.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}