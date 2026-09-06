using Application.DTOs.PersonDTO;
using Domain.Enums;
using Presentation.Services.Api;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Presentation.Views.Windows;

public partial class PersonDetailsWindow : Window
{
    private readonly int _personId;
    private readonly IPeopleApiClient _peopleApiClient;

    public PersonDetailsWindow(
        int personId,
        IPeopleApiClient peopleApiClient)
    {
        InitializeComponent();

        if (personId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(personId));

        _personId = personId;

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(
                nameof(peopleApiClient));

        Loaded += PersonDetailsWindow_Loaded;
    }

    private async void PersonDetailsWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var result =
                await _peopleApiClient.GetByIdAsync(
                    _personId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    $"Person ID = {_personId}\n\n" +
                    $"Error = {result.Error}",
                    "Person Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            LoadPersonData(
                result.Value!);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error loading person data: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void LoadPersonData(
        PersonDto person)
    {
        LblPersonId.Text =
            person.PersonId.ToString();

        LblNationalNo.Text =
            person.NationalNo;

        LblFullName.Text =
            person.FullName;

        LblGender.Text =
            person.Gender.ToString();

        LblDateOfBirth.Text =
            person.DateOfBirth.ToString("dd/MM/yyyy");

        LblPhone.Text =
            person.Phone;

        LblEmail.Text =
            string.IsNullOrEmpty(person.Email)
                ? "N/A"
                : person.Email;

        LblAddress.Text =
            person.Address;

        LblCountry.Text =
            string.IsNullOrEmpty(person.CountryName)
                ? "N/A"
                : person.CountryName;

        LoadImage(person);
    }

    private void LoadImage(PersonDto person)
    {
        try
        {
            var path =
                person.ImagePath?.Trim();

            if (!string.IsNullOrEmpty(path) &&
                File.Exists(path))
            {
                var bitmap =
                    new BitmapImage();

                bitmap.BeginInit();

                bitmap.UriSource =
                    new Uri(
                        path,
                        UriKind.Absolute);

                bitmap.CacheOption =
                    BitmapCacheOption.OnLoad;

                bitmap.EndInit();
                bitmap.Freeze();

                ImgPerson.Source = bitmap;

                return;
            }

            LoadDefaultImage(person.Gender);
        }
        catch
        {
            LoadDefaultImage(person.Gender);
        }
    }

    private void LoadDefaultImage(
        Gender gender)
    {
        var defaultImage =
            gender == Gender.Male
                ? "pack://application:,,,/Resources/Default_Male.png"
                : "pack://application:,,,/Resources/Default_Female.png";

        ImgPerson.Source =
            new BitmapImage(
                new Uri(
                    defaultImage,
                    UriKind.Absolute));
    }

    private void Close_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}