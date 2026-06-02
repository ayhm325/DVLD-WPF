using System.Windows;
using System.Windows.Controls;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class AddEditPersonPage : Page
    {
        public AddEditPersonPage(PersonViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            // الاشتراك بالحدث لمعالجة الرسائل في طبقة الـ View فقط
            viewModel.SaveCompleted += VM_SaveCompleted;
        }

        private void VM_SaveCompleted(bool isSuccess)
        {
            if (isSuccess)
            {
                MessageBox.Show("Saved Successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save person. Please check validations.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}