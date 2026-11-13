using DVLD.Contracts.License;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Presentation.Views.Controls;

public partial class DriverLicenseInfo : UserControl
{
    private int _photoLoadVersion;

    public DriverLicenseInfo() => InitializeComponent();

    public DriverLicenseInfoResponse? License
    {
        get => (DriverLicenseInfoResponse?)GetValue(LicenseProperty);
        set => SetValue(LicenseProperty, value);
    }

    public static readonly DependencyProperty LicenseProperty =
        DependencyProperty.Register(
            nameof(License),
            typeof(DriverLicenseInfoResponse),
            typeof(DriverLicenseInfo),
            new PropertyMetadata(null, OnLicenseChanged));

    public ImageSource? PersonPhoto
    {
        get => (ImageSource?)GetValue(PersonPhotoProperty);
        private set => SetValue(PersonPhotoProperty, value);
    }

    public static readonly DependencyProperty PersonPhotoProperty =
        DependencyProperty.Register(
            nameof(PersonPhoto),
            typeof(ImageSource),
            typeof(DriverLicenseInfo),
            new PropertyMetadata(null));

    private static async void OnLicenseChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var control = (DriverLicenseInfo)sender;
        var version = ++control._photoLoadVersion;
        var license = e.NewValue as DriverLicenseInfoResponse;
        control.PersonPhoto = null;

        if (license is null || string.IsNullOrWhiteSpace(license.ImagePath) || App.ServiceProvider is null)
            return;

        try
        {
            var photos = App.ServiceProvider.GetRequiredService<PersonPhotoCache>();
            var peopleApi = App.ServiceProvider.GetRequiredService<IPeopleApiClient>();
            var image = await photos.GetAsync(license.PersonId, peopleApi);
            if (version == control._photoLoadVersion && control.License?.PersonId == license.PersonId)
                control.PersonPhoto = image;
        }
        catch
        {
            if (version == control._photoLoadVersion)
                control.PersonPhoto = null;
        }
    }

    public string LicenseIdText
    {
        get => (string)GetValue(LicenseIdTextProperty);
        set => SetValue(LicenseIdTextProperty, value);
    }

    public static readonly DependencyProperty LicenseIdTextProperty =
        DependencyProperty.Register(
            nameof(LicenseIdText),
            typeof(string),
            typeof(DriverLicenseInfo));

    public ICommand? SearchCommand
    {
        get => (ICommand?)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public static readonly DependencyProperty SearchCommandProperty =
        DependencyProperty.Register(
            nameof(SearchCommand),
            typeof(ICommand),
            typeof(DriverLicenseInfo),
            new PropertyMetadata(null));

    public bool IsLicenseIdReadOnly
    {
        get => (bool)GetValue(IsLicenseIdReadOnlyProperty);
        set => SetValue(IsLicenseIdReadOnlyProperty, value);
    }

    public static readonly DependencyProperty IsLicenseIdReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsLicenseIdReadOnly),
            typeof(bool),
            typeof(DriverLicenseInfo),
            new PropertyMetadata(false));
}
