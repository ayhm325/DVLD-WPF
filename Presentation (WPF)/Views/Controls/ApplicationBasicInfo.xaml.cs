using DVLD.Contracts.Application;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls;

public partial class ApplicationBasicInfo : UserControl
{
    public event Action<int>? OpenPersonRequested;

    public ApplicationBasicInfo()
    {
        InitializeComponent();
    }

    public ApplicationBasicInfoResponse? Application
    {
        get => (ApplicationBasicInfoResponse?)GetValue(ApplicationProperty);
        set => SetValue(ApplicationProperty, value);
    }

    public static readonly DependencyProperty ApplicationProperty =
        DependencyProperty.Register(
            nameof(Application),
            typeof(ApplicationBasicInfoResponse),
            typeof(ApplicationBasicInfo),
            new PropertyMetadata(null, OnApplicationChanged));

    private static void OnApplicationChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is ApplicationBasicInfo control)
        {
            // Reserved for future UI refresh logic.
        }
    }

    private void PersonInfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (Application is null)
            return;

        var personId = Application.ApplicantPersonId;

        if (personId == 0)
            return;

        OpenPersonRequested?.Invoke(personId);
    }
}