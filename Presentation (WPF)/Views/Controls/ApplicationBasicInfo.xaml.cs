using Application.DTOs;
using System.Windows;
using System.Windows.Controls;


namespace Presentation.Views.Controls
{
    public partial class ApplicationBasicInfo : UserControl
    {
        public event Action<int> OpenPersonRequested;

        public ApplicationBasicInfo()
        {
            InitializeComponent();
        }


        public ApplicationBasicInfoDto Application
        {
            get => (ApplicationBasicInfoDto)GetValue(ApplicationProperty);
            set => SetValue(ApplicationProperty, value);
        }

        public static readonly DependencyProperty ApplicationProperty =
            DependencyProperty.Register(
                nameof(Application),
                typeof(ApplicationBasicInfoDto),
                typeof(ApplicationBasicInfo),
                new PropertyMetadata(null, OnApplicationChanged)); 

        private static void OnApplicationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // هذا الجزء اختياري: يُستخدم إذا كنت تريد تنفيذ منطق برمجي 
            // إضافي عند وصول بيانات جديدة للـ UserControl
            if (d is ApplicationBasicInfo control)
            {
                // يمكنك هنا عمل Refresh للبيانات إذا لزم الأمر
            }
        }

        private void PersonInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application == null)
                return;

            var personId = Application.ApplicantPersonID;

            if (personId == 0)
                return;

            OpenPersonRequested?.Invoke(personId);
        }
    }
}