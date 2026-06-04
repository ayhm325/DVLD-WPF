using System.Windows;
using System.Windows.Controls;

namespace Presentation.Helpers
{
    public static class PasswordBoxHelper
    {
        // Password Property
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordBoxHelper),
                new PropertyMetadata(string.Empty, PasswordPropertyChanged));

        private static void PasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox pb && pb.Password != (string)e.NewValue)
                pb.Password = (string)e.NewValue;
        }

        public static void SetPassword(DependencyObject d, string value) => d.SetValue(PasswordProperty, value);
        public static string GetPassword(DependencyObject d) => (string)d.GetValue(PasswordProperty); // إضافة Get

        // Attach Property
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, (d, e) => {
                    if (d is PasswordBox pb) pb.PasswordChanged += (s, ev) => SetPassword(pb, pb.Password);
                }));

        public static void SetAttach(DependencyObject d, bool value) => d.SetValue(AttachProperty, value);
        public static bool GetAttach(DependencyObject d) => (bool)d.GetValue(AttachProperty); // إضافة Get
    }
}