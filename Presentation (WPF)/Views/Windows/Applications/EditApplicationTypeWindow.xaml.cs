using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows.Applications
{
    /// <summary>
    /// Interaction logic for EditApplicationTypeWindow.xaml
    /// </summary>
    public partial class EditApplicationTypeWindow : Window
    {
        

        public EditApplicationTypeWindow( UpdateApplicationTypeViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }


        protected override async void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // افترضنا أنك تمرر الـ ID بطريقة ما، يمكنك استبداله بالـ ID الفعلي
            // await _viewModel.InitializeAsync(id_المطلوب); 
        }

    }
}
