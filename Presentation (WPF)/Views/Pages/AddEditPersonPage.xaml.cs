using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.Win32;
using Presentation.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views
{
    public partial class AddEditPersonPage : Page
    {
        private readonly PersonRepository _personRepository;
        private readonly CountryRepository _countryRepository;

        private Person _person;
        private bool _isEditMode = false;

       

        private PersonViewModel VM => (PersonViewModel)DataContext;

        public AddEditPersonPage(Person person = null )
        {
            InitializeComponent();

            var context = new DVLDDbContext();
            _personRepository = new PersonRepository(context);
            _countryRepository = new CountryRepository(context);

            // مهم جداً: أولاً ViewModel
            DataContext = new PersonViewModel();

            // ثم الدول
            VM.Countries = new ObservableCollection<Country>(_countryRepository.GetAll());

            if (person != null)
            {
                _isEditMode = true;
                _person = person;
                LoadData(person);
            }
            else
            {
                _person = new Person();
            }
        }

       

        private void LoadData(Person p)
        {
            VM.FirstName = p.FirstName;
            VM.SecondName = p.SecondName;
            VM.ThirdName = p.ThirdName;
            VM.LastName = p.LastName;

            VM.NationalNo = p.NationalNo;
            VM.Phone = p.Phone;
            VM.Email = p.Email;
            VM.Address = p.Address;

            VM.DateOfBirth = p.DateOfBirth;
            VM.IsMale = p.Gender == Gender.Male;
            VM.IsFemale = p.Gender == Gender.Female;

            VM.SelectedCountry = p.Country;
            VM.ImagePath = p.ImagePath;
        }

        // ===== SAVE =====
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var person = new Person
                {
                    PersonId = _person?.PersonId ?? 0,

                    FirstName = VM.FirstName,
                    SecondName = VM.SecondName,
                    ThirdName = VM.ThirdName,
                    LastName = VM.LastName,

                    NationalNo = VM.NationalNo,
                    Phone = VM.Phone,
                    Email = VM.Email,
                    Address = VM.Address,

                    DateOfBirth = VM.DateOfBirth,
                    Gender = VM.IsMale
                         ? Gender.Male
                         : Gender.Female,

                    // ❗ مؤقتاً (سنصلحه بعد ربط الكومبوبوكس)
                    NationalityCountryID = VM.SelectedCountry?.CountryId ?? 0,

                    ImagePath = VM.ImagePath
                };

                if (!Application.Validators.PersonValidator.Validate(person, out string error))
                {
                    MessageBox.Show(error);
                    return;
                }

                int result = _isEditMode
                    ? (_personRepository.UpdatePerson(person) ? 1 : -1)
                    : _personRepository.AddPerson(person);

                if (result <= 0)
                {
                    MessageBox.Show("Save failed");
                    return;
                }

                MessageBox.Show("Saved Successfully");
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void ChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.png;*.jpeg"
            };

            if (dlg.ShowDialog() == true)
            {
                VM.ImagePath = dlg.FileName;
            }
        }
    }
}