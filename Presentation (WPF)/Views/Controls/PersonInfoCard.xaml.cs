using Microsoft.Win32;
using Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls
{
    public partial class PersonInfoCard : UserControl
    {
        public PersonInfoCard()
        {
            InitializeComponent();
        }

        private void btnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Select Profile Image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures)
            };

            if (dlg.ShowDialog() == true)
            {
                // استدعاء الكومان الجاهز وتمرير المسار له مباشرة بدون Reflection!
                if (this.DataContext is PersonViewModel vm)
                {
                    vm.ChooseImageCommand.Execute(dlg.FileName);
                }
            }
        }

        private void btnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            // لمسح مسار الصورة
            var dataContext = this.DataContext;
            if (dataContext != null)
            {
                var prop = dataContext.GetType().GetProperty("ImagePath");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(dataContext, null);
                }
            }
        }
    }
}