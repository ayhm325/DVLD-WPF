using Application.DTOs;
using Application.Interfaces;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Presentation.Views.Windows
{
    public partial class PersonDetailsWindow : Window
    {
        private readonly IPersonService _personService;
        private readonly int _personId;

        public PersonDetailsWindow(IPersonService personService, int personId)
        {
            InitializeComponent();

            _personService = personService;
            _personId = personId;

            this.Loaded += PersonDetailsWindow_Loaded;
        }

        // ================= LOAD EVENT =================
        private async void PersonDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var fullPersonDto = await _personService.GetPersonByIdAsync(_personId);

                LoadPersonData(fullPersonDto!);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading person data: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ================= LOAD UI DATA =================
        private void LoadPersonData(PersonDto person)
        {
            if (person == null)
            {
                MessageBox.Show("Person data could not be found.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            LblPersonId.Text = person.PersonId.ToString();
            LblNationalNo.Text = person.NationalNo;
            LblFullName.Text = person.FullName;
            LblGender.Text = person.Gender;
            LblDateOfBirth.Text = person.DateOfBirth.ToString("dd/MM/yyyy");
            LblPhone.Text = person.Phone;
            LblEmail.Text = string.IsNullOrEmpty(person.Email) ? "N/A" : person.Email;
            LblAddress.Text = person.Address;
            LblCountry.Text = string.IsNullOrEmpty(person.CountryName) ? "N/A" : person.CountryName;

            LoadImage(person);
        }

        // ================= IMAGE HANDLING =================
        private void LoadImage(PersonDto person)
        {
            string? path = person.ImagePath?.Trim();

            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ImgPerson.Source = bitmap;
                }
                else
                {
                    LoadDefaultImage(person.Gender);
                }
            }
            catch (Exception)
            {
                // في حال حدوث أي خطأ غير متوقع في قراءة الملف
                LoadDefaultImage(person.Gender);
            }
        }
        

        // ================= DEFAULT IMAGE =================
        private void LoadDefaultImage(string gender)
        {
            string defaultImage = gender == "Male"
                ? "pack://application:,,,/Resources/Default_Male.png"
                : "pack://application:,,,/Resources/Default_Female.png";

            ImgPerson.Source = new BitmapImage(new Uri(defaultImage, UriKind.Absolute));
        }

        // ================= CLOSE =================
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}