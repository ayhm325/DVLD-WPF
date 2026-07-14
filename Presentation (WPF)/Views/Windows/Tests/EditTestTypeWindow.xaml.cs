
using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows.Tests
{
    /// <summary>
    /// Interaction logic for EditTestTypeWindow.xaml
    /// </summary>
    public partial class EditTestTypeWindow : Window
    {
        public EditTestTypeWindow(UpdateTestTypeViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        protected override  void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // افترضنا أنك تمرر الـ ID بطريقة ما، يمكنك استبداله بالـ ID الفعلي
            // await _viewModel.InitializeAsync(id_المطلوب); 
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

}
