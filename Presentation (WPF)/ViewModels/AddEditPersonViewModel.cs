
using Application.DTOs.PersonDTO;
using Application.DTOs.CountryDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using DVLD_WPF;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class AddEditPersonViewModel : ObservableObject
    {
        // =========================================================
        // EVENTS
        // =========================================================

        public event Action<bool>? SaveCompleted;


        // =========================================================
        // DEPENDENCIES
        // =========================================================

        private readonly IPersonService _personService;
        private readonly ICountryService _countryService;

        private readonly string _destinationFolder =
            @"C:\ImageDVLD\";


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public AddEditPersonViewModel(
            IPersonService personService,
            ICountryService countryService)
        {
            _personService =
                personService
                ?? throw new ArgumentNullException(
                    nameof(personService));

            _countryService =
                countryService
                ?? throw new ArgumentNullException(
                    nameof(countryService));
        }


        // =========================================================
        // BASIC PROPERTIES
        // =========================================================

        [ObservableProperty]
        private int _personId;


        [ObservableProperty]
        private OperationMode _mode;


        [ObservableProperty]
        private string _pageTitle =
            "Add Person";


        [ObservableProperty]
        private CountryDto? _selectedCountry;


        [ObservableProperty]
        private string _imagePath =
            string.Empty;


        // =========================================================
        // PERSON INFORMATION
        // =========================================================

        [ObservableProperty]
        private string _firstName =
            string.Empty;


        [ObservableProperty]
        private string _secondName =
            string.Empty;


        [ObservableProperty]
        private string _thirdName =
            string.Empty;


        [ObservableProperty]
        private string _lastName =
            string.Empty;


        [ObservableProperty]
        private string _nationalNo =
            string.Empty;


        [ObservableProperty]
        private string _phone =
            string.Empty;


        [ObservableProperty]
        private string _email =
            string.Empty;


        [ObservableProperty]
        private string _address =
            string.Empty;


        [ObservableProperty]
        private DateTime _dateOfBirth =
            DateTime.Today.AddYears(-18);


        // =========================================================
        // GENDER
        // =========================================================

        [ObservableProperty]
        private bool _isMale = true;


        [ObservableProperty]
        private bool _isFemale;


        // =========================================================
        // COUNTRIES
        // =========================================================

        public ObservableCollection<CountryDto> Countries { get; } = new();


        // =========================================================
        // UI PROPERTIES
        // =========================================================

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
                }
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x)));


        public string CountryName =>
            SelectedCountry?.CountryName
            ?? "Unknown";


        public string ImageDisplayPath =>
            !string.IsNullOrWhiteSpace(ImagePath)
                ? ImagePath
                : IsMale
                    ? "pack://application:,,,/Resources/Default_Male.png"
                    : "pack://application:,,,/Resources/Default_Female.png";


        // =========================================================
        // GENDER CHANGES
        // =========================================================

        partial void OnIsMaleChanged(bool value)
        {
            if (value)
            {
                IsFemale = false;
            }

            OnPropertyChanged(
                nameof(ImageDisplayPath));
        }


        partial void OnIsFemaleChanged(bool value)
        {
            if (value)
            {
                IsMale = false;
            }

            OnPropertyChanged(
                nameof(ImageDisplayPath));
        }


        // =========================================================
        // NAME CHANGES
        // =========================================================

        partial void OnFirstNameChanged(string value)
        {
            OnPropertyChanged(
                nameof(FullName));
        }


        partial void OnSecondNameChanged(string value)
        {
            OnPropertyChanged(
                nameof(FullName));
        }


        partial void OnThirdNameChanged(string value)
        {
            OnPropertyChanged(
                nameof(FullName));
        }


        partial void OnLastNameChanged(string value)
        {
            OnPropertyChanged(
                nameof(FullName));
        }


        partial void OnSelectedCountryChanged(
            CountryDto? value)
        {
            OnPropertyChanged(
                nameof(CountryName));
        }


        // =========================================================
        // INITIALIZE
        // =========================================================

        public async Task InitializeAsync(
            int? personId)
        {
            try
            {
                // -------------------------------------------------
                // Load countries
                // -------------------------------------------------

                var countriesResult =
                    await _countryService
                        .GetAllCountriesAsync();

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

                foreach (var country
                    in countriesResult.Value!)
                {
                    Countries.Add(country);
                }


                // -------------------------------------------------
                // EDIT MODE
                // -------------------------------------------------

                if (personId.HasValue &&
                    personId.Value > 0)
                {
                    var personResult =
                        await _personService
                            .GetPersonByIdAsync(
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

                    Mode =
                        OperationMode.Edit;

                    PersonId =
                        personId.Value;

                    PageTitle =
                        "Edit Person";

                    LoadData(
                        personResult.Value!);

                    return;
                }


                // -------------------------------------------------
                // ADD MODE
                // -------------------------------------------------

                Mode =
                    OperationMode.Add;

                PersonId = 0;

                PageTitle =
                    "Add Person";


                SelectedCountry =
                    Countries.FirstOrDefault(
                        c =>
                            c.CountryName == "Jordan")
                    ?? Countries.FirstOrDefault();


                // -------------------------------------------------
                // Default values
                // -------------------------------------------------

                FirstName =
                    string.Empty;

                SecondName =
                    string.Empty;

                ThirdName =
                    string.Empty;

                LastName =
                    string.Empty;

                NationalNo =
                    string.Empty;

                Phone =
                    string.Empty;

                Email =
                    string.Empty;

                Address =
                    string.Empty;

                DateOfBirth =
                    DateTime.Today.AddYears(-18);

                IsMale = true;
                IsFemale = false;

                ImagePath =
                    string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // LOAD EXISTING PERSON
        // =========================================================

        private void LoadData(
            PersonDto person)
        {
            if (person is null)
                return;


            // -----------------------------------------------------
            // NAME
            // -----------------------------------------------------

            // IMPORTANT:
            // Do NOT split FullName.
            //
            // The database stores the four names separately.
            // PersonDto now exposes them separately as well.

            FirstName =
                person.FirstName;

            SecondName =
                person.SecondName;

            ThirdName =
                person.ThirdName
                ?? string.Empty;

            LastName =
                person.LastName;


            // -----------------------------------------------------
            // OTHER DATA
            // -----------------------------------------------------

            NationalNo =
                person.NationalNo;

            Phone =
                person.Phone;

            Email =
                person.Email
                ?? string.Empty;

            Address =
                person.Address;

            DateOfBirth =
                person.DateOfBirth;


            // -----------------------------------------------------
            // GENDER
            // -----------------------------------------------------

            IsMale =
                person.Gender == Gender.Male;

            IsFemale =
                person.Gender == Gender.Female;


            // -----------------------------------------------------
            // COUNTRY
            // -----------------------------------------------------

            SelectedCountry =
                Countries.FirstOrDefault(
                    c =>
                        c.CountryId ==
                        person.NationalityCountryID)
                ?? Countries.FirstOrDefault();


            // -----------------------------------------------------
            // IMAGE
            // -----------------------------------------------------

            ImagePath =
                person.ImagePath
                ?? string.Empty;


            OnPropertyChanged(
                nameof(FullName));

            OnPropertyChanged(
                nameof(CountryName));

            OnPropertyChanged(
                nameof(ImageDisplayPath));
        }


        // =========================================================
        // SAVE
        // =========================================================

        [RelayCommand]
        private async Task SavePersonAsync()
        {
            try
            {
                // -------------------------------------------------
                // BASIC UI CHECKS
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(FirstName))
                {
                    ShowValidationMessage(
                        "First name is required.");

                    return;
                }

                if (string.IsNullOrWhiteSpace(SecondName))
                {
                    ShowValidationMessage(
                        "Second name is required.");

                    return;
                }

                if (string.IsNullOrWhiteSpace(LastName))
                {
                    ShowValidationMessage(
                        "Last name is required.");

                    return;
                }

                if (string.IsNullOrWhiteSpace(NationalNo))
                {
                    ShowValidationMessage(
                        "National number is required.");

                    return;
                }

                if (string.IsNullOrWhiteSpace(Phone))
                {
                    ShowValidationMessage(
                        "Phone number is required.");

                    return;
                }

                if (SelectedCountry is null)
                {
                    ShowValidationMessage(
                        "Nationality country is required.");

                    return;
                }


                // -------------------------------------------------
                // GENDER
                // -------------------------------------------------

                var gender =
                    IsMale
                        ? Gender.Male
                        : Gender.Female;


                // =================================================
                // EDIT
                // =================================================

                if (Mode == OperationMode.Edit)
                {
                    var updateDto =
                        new PersonUpdateDto
                        {
                            FirstName =
                                FirstName.Trim(),

                            SecondName =
                                SecondName.Trim(),

                            ThirdName =
                                string.IsNullOrWhiteSpace(
                                    ThirdName)
                                    ? null
                                    : ThirdName.Trim(),

                            LastName =
                                LastName.Trim(),

                            NationalNo =
                                NationalNo.Trim(),

                            Phone =
                                Phone.Trim(),

                            Email =
                                string.IsNullOrWhiteSpace(
                                    Email)
                                    ? null
                                    : Email.Trim(),

                            Address =
                                Address.Trim(),

                            DateOfBirth =
                                DateOfBirth,

                            Gender =
                                gender,

                            NationalityCountryID =
                                SelectedCountry.CountryId,

                            ImagePath =
                                string.IsNullOrWhiteSpace(
                                    ImagePath)
                                    ? null
                                    : ImagePath.Trim()
                        };


                    var updateResult =
                        await _personService
                            .UpdatePersonAsync(
                                PersonId,
                                updateDto);


                    if (updateResult.IsFailure)
                    {
                        MessageBox.Show(
                            updateResult.Error,
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

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


                // =================================================
                // ADD
                // =================================================

                var createDto =
                    new PersonCreateDto
                    {
                        FirstName =
                            FirstName.Trim(),

                        SecondName =
                            SecondName.Trim(),

                        ThirdName =
                            string.IsNullOrWhiteSpace(
                                ThirdName)
                                ? null
                                : ThirdName.Trim(),

                        LastName =
                            LastName.Trim(),

                        NationalNo =
                            NationalNo.Trim(),

                        Phone =
                            Phone.Trim(),

                        Email =
                            string.IsNullOrWhiteSpace(
                                Email)
                                ? null
                                : Email.Trim(),

                        Address =
                            Address.Trim(),

                        DateOfBirth =
                            DateOfBirth,

                        Gender =
                            gender,

                        NationalityCountryID =
                            SelectedCountry.CountryId,

                        ImagePath =
                            string.IsNullOrWhiteSpace(
                                ImagePath)
                                ? null
                                : ImagePath.Trim()
                    };


                var addResult =
                    await _personService
                        .AddPersonAsync(
                            createDto);


                if (addResult.IsFailure)
                {
                    MessageBox.Show(
                        addResult.Error,
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

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
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                SaveCompleted?.Invoke(false);
            }
        }


        // =========================================================
        // CHOOSE IMAGE
        // =========================================================

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
                if (!Directory.Exists(
                    _destinationFolder))
                {
                    Directory.CreateDirectory(
                        _destinationFolder);
                }

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

                ImagePath =
                    targetPath;

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


        // =========================================================
        // REMOVE IMAGE
        // =========================================================

        [RelayCommand]
        private void RemoveImage()
        {
            ImagePath =
                string.Empty;

            OnPropertyChanged(
                nameof(ImageDisplayPath));
        }


        // =========================================================
        // CANCEL
        // =========================================================

        [RelayCommand]
        private void Cancel()
        {
            MainWindow.Navigation.GoBack();
        }


        // =========================================================
        // HELPER
        // =========================================================

        private static void ShowValidationMessage(
            string message)
        {
            MessageBox.Show(
                message,
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
