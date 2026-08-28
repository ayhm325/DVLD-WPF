using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Presentation.ViewModels;
using Presentation.Views.Controls;
using System;
using System.Windows;
using System.Windows.Controls;


namespace Presentation.Views.Windows
{
    public partial class LocalApplicationDetailsWin : Window
    {
       
        public LocalApplicationDetailsWin(LocalApplicationDetailsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;


            ApplicationBasicInfoControl.OpenPersonRequested += OnOpenPersonRequested;
            DrivingLicenseApplicationInfoControl.OpenLicenseRequested += OnOpenLicenseRequested;
        }

        private void OnOpenPersonRequested(int personId)
        {
            var window = new PersonDetailsWindow(personId);
            window.ShowDialog();
        }

        private void OnOpenLicenseRequested(int licenseId)
        {
            if (licenseId <= 0)
                return;

            var window = new DriverLicenseInfoWin(licenseId)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}