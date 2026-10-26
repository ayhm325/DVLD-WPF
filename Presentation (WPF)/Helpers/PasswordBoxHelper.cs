using System.Windows;
using System.Windows.Controls;

namespace Presentation.Helpers;

public static class PasswordBoxHelper
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordPropertyChanged));

    public static readonly DependencyProperty AttachProperty =
        DependencyProperty.RegisterAttached(
            "Attach",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false, OnAttachPropertyChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxHelper));

    public static void SetPassword(DependencyObject d, string value) => d.SetValue(PasswordProperty, value);
    public static string GetPassword(DependencyObject d) => (string)d.GetValue(PasswordProperty) ?? string.Empty;

    public static void SetAttach(DependencyObject d, bool value) => d.SetValue(AttachProperty, value);
    public static bool GetAttach(DependencyObject d) => (bool)d.GetValue(AttachProperty);

    private static bool GetIsUpdating(DependencyObject d) => (bool)d.GetValue(IsUpdatingProperty);
    private static void SetIsUpdating(DependencyObject d, bool value) => d.SetValue(IsUpdatingProperty, value);

    private static void OnAttachPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox pb)
            return;

        if ((bool)e.OldValue)
            pb.PasswordChanged -= OnPasswordChanged;

        if ((bool)e.NewValue)
            pb.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox pb)
            return;

        if (GetIsUpdating(pb))
            return;

        SetIsUpdating(pb, true);
        try
        {
            var newPass = (string?)e.NewValue ?? string.Empty;
            if (pb.Password != newPass)
                pb.Password = newPass;
        }
        finally
        {
            SetIsUpdating(pb, false);
        }
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox pb)
            return;

        if (GetIsUpdating(pb))
            return;

        SetIsUpdating(pb, true);
        try
        {
            SetPassword(pb, pb.Password);
        }
        finally
        {
            SetIsUpdating(pb, false);
        }
    }
}
