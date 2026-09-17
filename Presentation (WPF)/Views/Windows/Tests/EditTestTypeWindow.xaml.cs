using Presentation.ViewModels;
using System.Windows;

namespace Presentation.Views.Windows.Tests;

public partial class EditTestTypeWindow : Window
{
    public EditTestTypeWindow(UpdateTestTypeViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}