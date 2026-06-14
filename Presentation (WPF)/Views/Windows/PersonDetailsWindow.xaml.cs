using Application.DTOs;
using Application.Interfaces;
using DVLD_WPF;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Views.Windows
{
    public partial class PersonDetailsWindow : Window
    {
        private readonly int _personId;
        private readonly IPersonService _personService;

        public PersonDetailsWindow(int personId)
        {
            InitializeComponent();

            _personId = personId;

            // الحصول على الخدمة من DI (بدون تمريرها من الخارج)
            _personService = App.ServiceProvider.GetRequiredService<IPersonService>();

            Loaded += PersonDetailsWindow_Loaded;
        }

        // ================= LOAD =================
        private async void PersonDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var fullPersonDto =
                    await _personService.GetPersonByIdAsync(_personId);

                if (fullPersonDto == null)
                {
                    MessageBox.Show("Person not found",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                LoadPersonData(fullPersonDto);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading person data: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ================= UI =================
        private void LoadPersonData(PersonDto person)
        {
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

        // ================= IMAGE =================
        private void LoadImage(PersonDto person)
        {
            try
            {
                string? path = person.ImagePath?.Trim();

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
            catch
            {
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