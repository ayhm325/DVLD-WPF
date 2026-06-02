using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Presentation.ViewModels
{
    public partial class PersonViewModel : ObservableValidator
    {
        private readonly IPersonService _personService;
        private readonly ICountryService _countryService;
        private readonly string _destinationFolder = @"C:\ImageDVLD\";

        [ObservableProperty] private int _personId;
        [ObservableProperty] private PersonFormMode _mode;
        [ObservableProperty] private string _pageTitle = "Add Person";
        [ObservableProperty] private Country _selectedCountry = null!;
        [ObservableProperty]private string _imagePath = string.Empty;

        public string ImageDisplayPath => !string.IsNullOrEmpty(ImagePath)
            ? ImagePath
            : (IsMale ? "pack://application:,,,/Resources/Default_Male.png" : "pack://application:,,,/Resources/Default_Female.png");

        public ObservableCollection<Country> Countries { get; } = new();
        public DateTime MaxBirthDate => DateTime.Now.AddYears(-18);

        public event Action<bool>? SaveCompleted;

        public PersonViewModel(IPersonService personService, ICountryService countryService)
        {
            _personService = personService;
            _countryService = countryService;
        }

        public async Task InitializeAsync(int? personId)
        {
            var countries = await _countryService.GetAllCountriesAsync();
            Countries.Clear();
            foreach (var country in countries) Countries.Add(country);

            if (personId.HasValue && personId.Value > 0)
            {
                var person = await _personService.GetPersonByIdAsync(personId.Value);
                if (person != null)
                {
                    _mode = PersonFormMode.Edit;
                    _personId = personId.Value;
                    PageTitle = "Edit Person";
                    LoadData(person);
                }
            }
            else
            {
                _mode = PersonFormMode.Add;
                PageTitle = "Add Person";
                SelectedCountry = Countries.FirstOrDefault(c => c.CountryName == "Jordan") ?? Countries.FirstOrDefault()!;
            }
        }

        [ObservableProperty][CustomValidation(typeof(PersonViewModel), nameof(ValidateFirstNameField))] private string _firstName = string.Empty;
        [ObservableProperty][CustomValidation(typeof(PersonViewModel), nameof(ValidateSecondNameField))] private string _secondName = string.Empty;
        [ObservableProperty] private string _thirdName = string.Empty;
        [ObservableProperty][CustomValidation(typeof(PersonViewModel), nameof(ValidateLastNameField))] private string _lastName = string.Empty;
        [ObservableProperty][CustomValidation(typeof(PersonViewModel), nameof(ValidateNationalNoField))] private string _nationalNo = string.Empty;
        [ObservableProperty][CustomValidation(typeof(PersonViewModel), nameof(ValidatePhoneField))] private string _phone = string.Empty;
        [ObservableProperty][CustomValidation(typeof(PersonViewModel), nameof(ValidateEmailField))] private string _email = string.Empty;
        [ObservableProperty][CustomValidation(typeof(PersonViewModel), nameof(ValidateAddressField))] private string _address = string.Empty;

        [ObservableProperty]
        [CustomValidation(typeof(PersonViewModel), nameof(ValidateAge))]
        private DateTime _dateOfBirth = DateTime.Now.AddYears(-18);

        [ObservableProperty]
        private bool _isMale = true;

        [ObservableProperty]
        private bool _isFemale;

        partial void OnIsMaleChanged(bool value)
        {
            if (value)
                IsFemale = false;

            OnPropertyChanged(nameof(ImageDisplayPath));
        }

        partial void OnIsFemaleChanged(bool value)
        {
            if (value)
                IsMale = false;

            OnPropertyChanged(nameof(ImageDisplayPath));
        }

        private void LoadData(PersonDto p)
        {
            var nameParts = p.FullName?.Split(' ') ?? Array.Empty<string>();
            FirstName = nameParts.ElementAtOrDefault(0) ?? "";
            SecondName = nameParts.ElementAtOrDefault(1) ?? "";
            if (nameParts.Length == 3)
             {
                 // إذا كان الاسم 3 أجزاء فقط (أول، ثانٍ، أخير)
                 ThirdName = ""; // نترك الثالث فارغاً
                 LastName = nameParts.ElementAtOrDefault(2) ?? "";
             }
             else if (nameParts.Length >= 4)
             {
                 // إذا كان الاسم 4 أجزاء أو أكثر
                 ThirdName = nameParts.ElementAtOrDefault(2) ?? "";
                 LastName = nameParts.ElementAtOrDefault(3) ?? "";
             }
            

            NationalNo = p.NationalNo ?? "";
            Phone = p.Phone ?? "";
            Email = p.Email ?? "";
            Address = p.Address ?? "";
            DateOfBirth = p.DateOfBirth;
            IsMale = p.Gender == "Male";
            IsFemale = p.Gender == "Female";

            SelectedCountry = Countries.FirstOrDefault(c => c.CountryName == p.CountryName) ?? Countries.FirstOrDefault()!;
            ImagePath = p.ImagePath ?? "";

            ValidateAllProperties();
        }

        [RelayCommand]
        private async Task SavePersonAsync()
        {
            ValidateAllProperties();
            if (HasErrors) { SaveCompleted?.Invoke(false); return; }

            var dto = new PersonCreateUpdateDto
            {
                PersonId = this._personId,
                FirstName = FirstName,
                SecondName = SecondName,
                ThirdName = ThirdName,
                LastName = LastName,
                NationalNo = NationalNo,
                Phone = Phone,
                Email = Email,
                Address = Address,
                DateOfBirth = DateOfBirth,
                Gender = IsMale ? Gender.Male : Gender.Female,
                NationalityCountryID = SelectedCountry?.CountryId ?? 0,
                ImagePath = string.IsNullOrWhiteSpace(ImagePath) ? null : ImagePath
            };

            bool result = _mode == PersonFormMode.Edit
                ? await _personService.UpdatePersonAsync(_personId, dto)
                : await _personService.AddPersonAsync(dto) > 0;

            SaveCompleted?.Invoke(result);
        }

        [RelayCommand]
        private void ChooseImage()
        {
            var openFileDialog = new OpenFileDialog { Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp" };
            if (openFileDialog.ShowDialog() == true)
            {
                if (!Directory.Exists(_destinationFolder)) Directory.CreateDirectory(_destinationFolder);
                string targetPath = Path.Combine(_destinationFolder, Guid.NewGuid().ToString() + Path.GetExtension(openFileDialog.FileName));
                File.Copy(openFileDialog.FileName, targetPath, true);
                ImagePath = targetPath;
                OnPropertyChanged(nameof(ImageDisplayPath));
            }
        }

        [RelayCommand]
        private void RemoveImage()
        {
            ImagePath = string.Empty;
            OnPropertyChanged(nameof(ImageDisplayPath));
        }

        // --- الدوال المفقودة التي كانت تسبب الخطأ ---

        public static ValidationResult? ValidateAge(DateTime dateOfBirth, ValidationContext context)
        {
            if (dateOfBirth > DateTime.Now.AddYears(-18))
            {
                return new ValidationResult("Person must be at least 18 years old.");
            }
            return ValidationResult.Success;
        }

        public static ValidationResult? ValidateFirstNameField(string value, ValidationContext context) => ValidateField(value, "First name", context);
        public static ValidationResult? ValidateSecondNameField(string value, ValidationContext context) => ValidateField(value, "Second name", context);
        public static ValidationResult? ValidateLastNameField(string value, ValidationContext context) => ValidateField(value, "Last name", context);
        public static ValidationResult? ValidateNationalNoField(string value, ValidationContext context) => ValidateField(value, "National", context);
        public static ValidationResult? ValidatePhoneField(string value, ValidationContext context) => ValidateField(value, "Phone", context);
        public static ValidationResult? ValidateEmailField(string value, ValidationContext context) => string.IsNullOrWhiteSpace(value) ? ValidationResult.Success : ValidateField(value, "Email", context);
        public static ValidationResult? ValidateAddressField(string value, ValidationContext context) => ValidateField(value, "Address", context);


        private static ValidationResult? ValidateField(string value, string fieldName, ValidationContext context)
        {
            // الحصول على الـ ViewModel الحالي
            var vm = (PersonViewModel)context.ObjectInstance;

            // إنشاء كائن Person يحتوي على البيانات الحالية التي أدخلها المستخدم في الـ UI
            var personToValidate = new Person
            {
                FirstName = vm.FirstName,
                SecondName = vm.SecondName,
                ThirdName = vm.ThirdName,
                LastName = vm.LastName,
                NationalNo = vm.NationalNo,
                Phone = vm.Phone,
                Email = vm.Email,
                Address = vm.Address,
                Gender = vm.IsMale ? Gender.Male : Gender.Female
            };

            // استخدام الـ Validator الخاص بك للتحقق من الكائن الفعلي
            var res = PersonValidator.Validate(personToValidate);

            // البحث عن الخطأ المرتبط بهذا الحقل تحديداً
            var err = res.Errors.FirstOrDefault(e => e.Contains(fieldName, StringComparison.OrdinalIgnoreCase));

            return err != null ? new ValidationResult(err) : ValidationResult.Success;
        }
    }
}