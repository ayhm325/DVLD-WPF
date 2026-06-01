using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Presentation.Views;
using Infrastructure;
using Infrastructure.Repositories;
using Presentation.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views
{
    public partial class PeoplePage : Page
    {
        private readonly PersonRepository _personRepository;
        private List<Person> _allPeople;

        private bool _isLoaded = false;

        public PeoplePage()
        {
            InitializeComponent();

            var context = new DVLDDbContext();
            _personRepository = new PersonRepository(context);

            LoadPeople();

            _isLoaded = true;
        }

        // 📥 Load data
        private void LoadPeople()
        {
            _allPeople = _personRepository.GetAllPersons().ToList();
            dgPeople.ItemsSource = _allPeople;

            txtPeopleCount.Text = _allPeople.Count.ToString();
        }

        // 🔽 Filter Type
        private void cbFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            if (cbFilterType.SelectedItem is not ComboBoxItem item)
                return;

            string type = item.Content?.ToString() ?? "None";

            txtSearch.Visibility = Visibility.Collapsed;
            cbGender.Visibility = Visibility.Collapsed;

            txtSearch.Text = "";

            // 🔥 Dynamic ToolTip
            txtSearch.ToolTip = type switch
            {
                "Name" => "Search by Name",
                "National No" => "Search by National No",
                _ => "Search..."
            };

            if (type == "National No" || type == "Name")
                txtSearch.Visibility = Visibility.Visible;

            else if (type == "Gender")
                cbGender.Visibility = Visibility.Visible;

            ApplyFilter();
        }

        // 🔍 Text search
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        // ⚧ Gender change
        private void cbGender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        // ⚡ Filter logic
        private void ApplyFilter()
        {
            if (_allPeople == null)
                return;

            var filtered = _allPeople.AsQueryable();

            string filterType =
                (cbFilterType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";

            // NONE
            if (filterType == "None")
            {
                dgPeople.ItemsSource = _allPeople;
                return;
            }

            // NATIONAL NO
            if (filterType == "National No")
            {
                string search = txtSearch.Text?.ToLower() ?? "";

                filtered = filtered.Where(p =>
                    !string.IsNullOrEmpty(p.NationalNo) &&
                    p.NationalNo.ToLower().Contains(search));
            }

            // NAME
            else if (filterType == "Name")
            {
                string search = txtSearch.Text?.ToLower() ?? "";

                filtered = filtered.Where(p =>
                    (!string.IsNullOrEmpty(p.FullName) &&
                     p.FullName.ToLower().Contains(search))); 
            }

            // GENDER
            else if (filterType == "Gender")
            {
                if (cbGender.SelectedItem is ComboBoxItem item)
                {
                    string gender = item.Content?.ToString() ?? "All";

                    if (gender == "Male")
                        filtered = filtered.Where(p => p.Gender == Gender.Male);

                    else if (gender == "Female")
                        filtered = filtered.Where(p => p.Gender == Gender.Female);
                }
            }

            dgPeople.ItemsSource = filtered.ToList();

            var result = filtered.ToList();
            dgPeople.ItemsSource = result;

            txtPeopleCount.Text = result.Count.ToString();


        }

        private void AddPerson_Click(object sender, RoutedEventArgs e)
        {
            NavigationHelper.Navigate(new AddEditPersonPage());            
        }




    }
}