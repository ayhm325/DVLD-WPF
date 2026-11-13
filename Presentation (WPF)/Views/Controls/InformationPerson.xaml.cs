using DVLD.Contracts.Person;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Presentation.Views.Controls;

public partial class InformationPerson : UserControl
{
    private int _photoLoadVersion;

    public InformationPerson() => InitializeComponent();

    public PersonResponse? Person
    {
        get => (PersonResponse?)GetValue(PersonProperty);
        set => SetValue(PersonProperty, value);
    }

    public static readonly DependencyProperty PersonProperty =
        DependencyProperty.Register(
            nameof(Person),
            typeof(PersonResponse),
            typeof(InformationPerson),
            new PropertyMetadata(null, OnPersonChanged));

    public ImageSource? PersonImage
    {
        get => (ImageSource?)GetValue(PersonImageProperty);
        private set => SetValue(PersonImageProperty, value);
    }

    public static readonly DependencyProperty PersonImageProperty =
        DependencyProperty.Register(
            nameof(PersonImage),
            typeof(ImageSource),
            typeof(InformationPerson),
            new PropertyMetadata(null));

    private static async void OnPersonChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var control = (InformationPerson)sender;
        var version = ++control._photoLoadVersion;
        var person = e.NewValue as PersonResponse;
        control.PersonImage = CreateFallback(person?.Gender);

        if (person is null || string.IsNullOrWhiteSpace(person.ImagePath) || App.ServiceProvider is null)
            return;

        try
        {
            var photos = App.ServiceProvider.GetRequiredService<PersonPhotoCache>();
            var peopleApi = App.ServiceProvider.GetRequiredService<IPeopleApiClient>();
            var image = await photos.GetAsync(person.PersonId, peopleApi);
            if (version == control._photoLoadVersion && control.Person?.PersonId == person.PersonId)
                control.PersonImage = image ?? CreateFallback(person.Gender);
        }
        catch
        {
            if (version == control._photoLoadVersion)
                control.PersonImage = CreateFallback(person.Gender);
        }
    }

    private static BitmapImage CreateFallback(Gender? gender)
    {
        var resource = gender == Gender.Female ? "Default_Female.png" : "Default_Male.png";
        var bitmap = new BitmapImage(new Uri($"pack://application:,,,/Resources/{resource}", UriKind.Absolute));
        bitmap.Freeze();
        return bitmap;
    }
}
