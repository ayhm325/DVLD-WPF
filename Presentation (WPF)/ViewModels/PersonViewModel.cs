using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DVLD.Domain.Entities;

namespace Presentation.ViewModels
{
    public class PersonViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

       

        // ================= BASIC =================

        private string _nationalNo;
        public string NationalNo
        {
            get => _nationalNo;
            set { _nationalNo = value; OnPropertyChanged(nameof(NationalNo)); }
        }

        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(nameof(FirstName)); }
        }

        private string _secondName;
        public string SecondName
        {
            get => _secondName;
            set { _secondName = value; OnPropertyChanged(nameof(SecondName)); }
        }

        private string _thirdName;
        public string ThirdName
        {
            get => _thirdName;
            set { _thirdName = value; OnPropertyChanged(nameof(ThirdName)); }
        }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(nameof(LastName)); }
        }

        // ================= CONTACT =================

        private string _phone;
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(nameof(Phone)); }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(nameof(Address)); }
        }

        // ================= PERSONAL =================

        private DateTime _dateOfBirth = DateTime.Now;
        public DateTime DateOfBirth
        {
            get => _dateOfBirth;
            set { _dateOfBirth = value; OnPropertyChanged(nameof(DateOfBirth)); }
        }

        // ================= GENDER =================

        private bool _isMale = true;
        public bool IsMale
        {
            get => _isMale;
            set { _isMale = value; OnPropertyChanged(nameof(IsMale)); }
        }

        private bool _isFemale;
        public bool IsFemale
        {
            get => _isFemale;
            set { _isFemale = value; OnPropertyChanged(nameof(IsFemale)); }
        }

        // ================= COUNTRY =================

        public ObservableCollection<Country> Countries { get; set; } = new();

        private Country _selectedCountry;
        public Country SelectedCountry
        {
            get => _selectedCountry;
            set { _selectedCountry = value; OnPropertyChanged(nameof(SelectedCountry)); }
        }

        // ================= IMAGE =================

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }
    }
}