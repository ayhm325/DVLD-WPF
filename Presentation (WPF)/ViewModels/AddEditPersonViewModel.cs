using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Enums;
using DVLD.Contracts.Country;
using DVLD.Contracts.Person;
using DVLD_WPF;
using Microsoft.Win32;
using Presentation.Services.Api;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace Presentation.ViewModels;

public partial class AddEditPersonViewModel : ObservableObject
{
    public event Action<bool>? SaveCompleted;

    private readonly IPeopleApiClient _peopleApiClient;
    private readonly ICountriesApiClient _countriesApiClient;

    private readonly string _destinationFolder =
        @"C:\ImageDVLD\";

    [ObservableProperty]
    private int _personId;

    [ObservableProperty]
    private OperationMode _mode;

    [ObservableProperty]
    private string _pageTitle = "Add Person";

    [ObservableProperty]
    private CountryResponse? _selectedCountry;

    [ObservableProperty]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _secondName = string.Empty;

    [ObservableProperty]
    private string _thirdName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _nationalNo = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private DateTime _dateOfBirth =
        DateTime.Today.AddYears(-18);

    [ObservableProperty]
    private bool _isMale = true;

    [ObservableProperty]
    private bool _isFemale;

    public ObservableCollection<CountryResponse> Countries { get; } = new();

    public DateTime MaxBirthDate =>
        DateTime.Today.AddYears(-18);

    public string FullName =>
        string.Join(
            " ",
            new[]
            {
                FirstName,
                SecondName,
                ThirdName,
                LastName
            }.Where(
                x => !string.IsNullOrWhiteSpace(x)));

    public string CountryName =>
        SelectedCountry?.CountryName ?? "Unknown";

    public string ImageDisplayPath =>
        !string.IsNullOrWhiteSpace(ImagePath)
            ? ImagePath
            : IsMale
                ? "pack://application:,,,/Resources/Default_Male.png"
                : "pack://application:,,,/Resources/Default_Female.png";

    public AddEditPersonViewModel(
        IPeopleApiClient peopleApiClient,
        ICountriesApiClient countriesApiClient)
    {
        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(
                nameof(peopleApiClient));

        _countriesApiClient =
            countriesApiClient
            ?? throw new ArgumentNullException(
                nameof(countriesApiClient));
    }

    partial void OnIsMaleChanged(bool value)
    {
        if (value)
            IsFemale = false;

        OnPropertyChanged(
            nameof(ImageDisplayPath));
    }

    partial void OnIsFemaleChanged(bool value)
    {
        if (value)
            IsMale = false;

        OnPropertyChanged(
            nameof(ImageDisplayPath));
    }

    partial void OnFirstNameChanged(string value)
    {
        OnPropertyChanged(nameof(FullName));
    }

    partial void OnSecondNameChanged(string value)
    {
        OnPropertyChanged(nameof(FullName));
    }

    partial void OnThirdNameChanged(string value)
    {
        OnPropertyChanged(nameof(FullName));
    }

    partial void OnLastNameChanged(string value)
    {
        OnPropertyChanged(nameof(FullName));
    }

    partial void OnSelectedCountryChanged(
        CountryResponse? value)
    {
        OnPropertyChanged(nameof(CountryName));
    }

    public async Task InitializeAsync(int? personId)
    {
        var countriesResult =
            await _countriesApiClient.GetAllAsync();

        if (countriesResult.IsFailure)
        {
            MessageBox.Show(
                countriesResult.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        Countries.Clear();

        foreach (var country in countriesResult.Value ?? [])
            Countries.Add(country);

        if (personId is > 0)
        {
            var personResult =
                await _peopleApiClient.GetByIdAsync(
                    personId.Value);

            if (personResult.IsFailure)
            {
                MessageBox.Show(
                    personResult.Error,
                    "Person Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Mode = OperationMode.Edit;
            PersonId = personId.Value;
            PageTitle = "Edit Person";

            LoadData(personResult.Value!);

            return;
        }

        ResetForAdd();
    }

    private void ResetForAdd()
    {
        Mode = OperationMode.Add;
        PersonId = 0;
        PageTitle = "Add Person";

        SelectedCountry =
            Countries.FirstOrDefault(
                c => c.CountryName == "Jordan")
            ?? Countries.FirstOrDefault();

        FirstName = string.Empty;
        SecondName = string.Empty;
        ThirdName = string.Empty;
        LastName = string.Empty;
        NationalNo = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        Address = string.Empty;

        DateOfBirth =
            DateTime.Today.AddYears(-18);

        IsMale = true;
        IsFemale = false;
        ImagePath = string.Empty;
    }

    private void LoadData(PersonResponse person)
    {
        FirstName = person.FirstName;
        SecondName = person.SecondName;
        ThirdName = person.ThirdName ?? string.Empty;
        LastName = person.LastName;

        NationalNo = person.NationalNo;
        Phone = person.Phone;
        Email = person.Email ?? string.Empty;
        Address = person.Address;
        DateOfBirth = person.DateOfBirth;

        IsMale =
            person.Gender == DVLD.Contracts.Person.Gender.Male;

        IsFemale =
            person.Gender == DVLD.Contracts.Person.Gender.Female;

        SelectedCountry =
            Countries.FirstOrDefault(
                c => c.CountryId ==
                     person.NationalityCountryID)
            ?? Countries.FirstOrDefault();

        ImagePath =
            person.ImagePath ?? string.Empty;

        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(CountryName));
        OnPropertyChanged(nameof(ImageDisplayPath));
    }

    [RelayCommand]
    private async Task SavePersonAsync()
    {
        if (!ValidateInput())
            return;

        var gender =
            IsMale
                ? DVLD.Contracts.Person.Gender.Male
                : DVLD.Contracts.Person.Gender.Female;

        if (Mode == OperationMode.Edit)
        {
            var updateRequest =
                new UpdatePersonRequest
                {
                    FirstName =
                        FirstName.Trim(),

                    SecondName =
                        SecondName.Trim(),

                    ThirdName =
                        string.IsNullOrWhiteSpace(ThirdName)
                            ? null
                            : ThirdName.Trim(),

                    LastName =
                        LastName.Trim(),

                    NationalNo =
                        NationalNo.Trim(),

                    Phone =
                        Phone.Trim(),

                    Email =
                        string.IsNullOrWhiteSpace(Email)
                            ? null
                            : Email.Trim(),

                    Address =
                        Address.Trim(),

                    DateOfBirth =
                        DateOfBirth,

                    Gender =
                        gender,

                    NationalityCountryID =
                        SelectedCountry!.CountryId,

                    ImagePath =
                        string.IsNullOrWhiteSpace(ImagePath)
                            ? null
                            : ImagePath.Trim()
                };

            var result =
                await _peopleApiClient.UpdateAsync(
                    PersonId,
                    updateRequest);

            if (result.IsFailure)
            {
                ShowValidationMessage(
                    result.Error);

                SaveCompleted?.Invoke(false);

                return;
            }

            MessageBox.Show(
                "Person data has been updated successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            SaveCompleted?.Invoke(true);

            return;
        }

        var createRequest =
            new CreatePersonRequest
            {
                FirstName =
                    FirstName.Trim(),

                SecondName =
                    SecondName.Trim(),

                ThirdName =
                    string.IsNullOrWhiteSpace(ThirdName)
                        ? null
                        : ThirdName.Trim(),

                LastName =
                    LastName.Trim(),

                NationalNo =
                    NationalNo.Trim(),

                Phone =
                    Phone.Trim(),

                Email =
                    string.IsNullOrWhiteSpace(Email)
                        ? null
                        : Email.Trim(),

                Address =
                    Address.Trim(),

                DateOfBirth =
                    DateOfBirth,

                Gender =
                    gender,

                NationalityCountryID =
                    SelectedCountry!.CountryId,

                ImagePath =
                    string.IsNullOrWhiteSpace(ImagePath)
                        ? null
                        : ImagePath.Trim()
            };

        var addResult =
            await _peopleApiClient.CreateAsync(
                createRequest);

        if (addResult.IsFailure)
        {
            ShowValidationMessage(
                addResult.Error);

            SaveCompleted?.Invoke(false);

            return;
        }

        MessageBox.Show(
            "Person has been added successfully.",
            "Success",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        SaveCompleted?.Invoke(true);
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
            return ShowValidationMessage(
                "First name is required.");

        if (string.IsNullOrWhiteSpace(SecondName))
            return ShowValidationMessage(
                "Second name is required.");

        if (string.IsNullOrWhiteSpace(LastName))
            return ShowValidationMessage(
                "Last name is required.");

        if (string.IsNullOrWhiteSpace(NationalNo))
            return ShowValidationMessage(
                "National number is required.");

        if (string.IsNullOrWhiteSpace(Phone))
            return ShowValidationMessage(
                "Phone number is required.");

        if (SelectedCountry is null)
            return ShowValidationMessage(
                "Nationality country is required.");

        return true;
    }

    [RelayCommand]
    private void ChooseImage()
    {
        var dialog =
            new OpenFileDialog
            {
                Filter =
                    "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            Directory.CreateDirectory(
                _destinationFolder);

            var extension =
                Path.GetExtension(
                    dialog.FileName);

            var targetPath =
                Path.Combine(
                    _destinationFolder,
                    $"{Guid.NewGuid()}{extension}");

            File.Copy(
                dialog.FileName,
                targetPath,
                true);

            ImagePath = targetPath;

            OnPropertyChanged(
                nameof(ImageDisplayPath));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Image Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RemoveImage()
    {
        ImagePath = string.Empty;

        OnPropertyChanged(
            nameof(ImageDisplayPath));
    }

    [RelayCommand]
    private void Cancel()
    {
        MainWindow.Navigation.GoBack();
    }

    private static bool ShowValidationMessage(
        string message)
    {
        MessageBox.Show(
            message,
            "Validation Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        return false;
    }
}