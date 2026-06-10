using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Helpers;
using Presentation.ViewModels;
using Presentation.Views.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views
{
    public partial class PeoplePage : Page
    {
        // يقرأ الـ ViewModel الحالي المرتبط بالواجهة بشكل آمن
        private PeopleViewModel? _viewModel => DataContext as PeopleViewModel;

        public PeoplePage()
        {
            InitializeComponent();

            // ✅ التعديل الأول: الاستدعاء الصحيح للمتغير الـ static عبر اسم الكلاس مباشرة
            if (DVLD_WPF.App.ServiceProvider != null)
            {
                this.DataContext = DVLD_WPF.App.ServiceProvider.GetRequiredService<PeopleViewModel>();
            }

            this.IsVisibleChanged += PeoplePage_IsVisibleChanged;
        }

        private async void PeoplePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true && _viewModel != null)
            {
                // استدعاء دالة تحميل البيانات عند فتح الصفحة
                await _viewModel.LoadPeopleAsync();
            }
        }

        private async void AddPerson_Click(object sender, RoutedEventArgs e)
        {
            if (DVLD_WPF.App.ServiceProvider != null)
            {
                var addEditVm = DVLD_WPF.App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();

                // 🔴 هذا السطر مفقود في كود الـ Click الخاص بك!
                await addEditVm.InitializeAsync(null);

                var win = new AddEditPersonWin(addEditVm)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                win.ShowDialog();
            }
        }
    }
}