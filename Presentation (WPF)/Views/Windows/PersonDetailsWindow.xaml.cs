using DVLD.Contracts.Person;
using Presentation.Services.Api;
using Presentation.Services.Results;
using Presentation.Services.UI;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Presentation.Views.Windows;

public partial class PersonDetailsWindow : Window
{
    private readonly int _personId;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IApiNotificationService _notifications;

    public PersonDetailsWindow(
        int personId,
        IPeopleApiClient peopleApiClient,
        IApiNotificationService notifications)
    {
        InitializeComponent();

        if (personId <= 0)
            throw new ArgumentOutOfRangeException(nameof(personId));

        _personId = personId;
        _peopleApiClient = peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        Loaded += PersonDetailsWindow_Loaded;
    }

    private async void PersonDetailsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var result = await _peopleApiClient.GetByIdAsync(_personId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Person Loading Error");
            return;
        }

        if (result.Value is null)
        {
            _notifications.ShowFailure(
                ApiResult.Failure("Person information was not returned by the API."),
                "Person Loading Error");
            return;
        }

        LoadPersonData(result.Value);
    }

    private void LoadPersonData(PersonResponse person)
    {
        LblPersonId.Text = person.PersonId.ToString();
        LblNationalNo.Text = person.NationalNo;
        LblFullName.Text = person.FullName;
        LblGender.Text = person.Gender.ToString();
        LblDateOfBirth.Text = person.DateOfBirth.ToString("dd/MM/yyyy");
        LblPhone.Text = person.Phone;
        LblEmail.Text = string.IsNullOrEmpty(person.Email) ? "N/A" : person.Email;
        LblAddress.Text = person.Address;
        LblCountry.Text = string.IsNullOrEmpty(person.CountryName) ? "N/A" : person.CountryName;
        LoadImage(person);
    }

    private void LoadImage(PersonResponse person)
    {
        try
        {
            var path = person.ImagePath?.Trim();

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                ImgPerson.Source = bitmap;
                return;
            }
        }
        catch
        {
        }

        LoadDefaultImage(person.Gender);
    }

    private void LoadDefaultImage(Gender gender)
    {
        var image = gender == Gender.Male
            ? "pack://application:,,,/Resources/Default_Male.png"
            : "pack://application:,,,/Resources/Default_Female.png";

        ImgPerson.Source = new BitmapImage(new Uri(image, UriKind.Absolute));
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        Loaded -= PersonDetailsWindow_Loaded;
        base.OnClosed(e);
    }
}