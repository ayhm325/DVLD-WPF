using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Presentation.Views.Controls;

public partial class PersonPhotoAvatar : UserControl
{
    private int _loadVersion;

    public PersonPhotoAvatar()
    {
        InitializeComponent();
        PhotoSource = CreateFallback();
    }

    public int PersonId
    {
        get => (int)GetValue(PersonIdProperty);
        set => SetValue(PersonIdProperty, value);
    }

    public static readonly DependencyProperty PersonIdProperty =
        DependencyProperty.Register(
            nameof(PersonId),
            typeof(int),
            typeof(PersonPhotoAvatar),
            new PropertyMetadata(0, OnPhotoIdentityChanged));

    public bool? HasPhoto
    {
        get => (bool?)GetValue(HasPhotoProperty);
        set => SetValue(HasPhotoProperty, value);
    }

    public static readonly DependencyProperty HasPhotoProperty =
        DependencyProperty.Register(
            nameof(HasPhoto),
            typeof(bool?),
            typeof(PersonPhotoAvatar),
            new PropertyMetadata(null, OnPhotoIdentityChanged));

    public ImageSource? PhotoSource
    {
        get => (ImageSource?)GetValue(PhotoSourceProperty);
        private set => SetValue(PhotoSourceProperty, value);
    }

    public static readonly DependencyProperty PhotoSourceProperty =
        DependencyProperty.Register(
            nameof(PhotoSource),
            typeof(ImageSource),
            typeof(PersonPhotoAvatar),
            new PropertyMetadata(null));

    private static async void OnPhotoIdentityChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var control = (PersonPhotoAvatar)sender;
        var version = ++control._loadVersion;
        control.PhotoSource = CreateFallback();

        if (control.PersonId <= 0 || control.HasPhoto == false || App.ServiceProvider is null)
            return;

        try
        {
            var cache = App.ServiceProvider.GetRequiredService<PersonPhotoCache>();
            var peopleApi = App.ServiceProvider.GetRequiredService<IPeopleApiClient>();
            var image = await cache.GetAsync(control.PersonId, peopleApi);
            if (version == control._loadVersion)
                control.PhotoSource = image ?? CreateFallback();
        }
        catch
        {
            if (version == control._loadVersion)
                control.PhotoSource = CreateFallback();
        }
    }

    private static BitmapImage CreateFallback()
    {
        var image = new BitmapImage(new Uri("pack://application:,,,/Resources/DefaultAvatar.png", UriKind.Absolute));
        image.Freeze();
        return image;
    }
}
