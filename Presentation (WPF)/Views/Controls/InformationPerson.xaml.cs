using Application.DTOs;
using System.Windows;
using System.Windows.Controls;

namespace Presentation.Views.Controls
{
    public partial class InformationPerson : UserControl
    {
        public InformationPerson()
        {
            InitializeComponent();
        }

        public PersonDto? Person
        {
            get => (PersonDto?)GetValue(PersonProperty);
            set => SetValue(PersonProperty, value);
        }

        public static readonly DependencyProperty PersonProperty =
            DependencyProperty.Register(
                nameof(Person),
                typeof(PersonDto),
                typeof(InformationPerson),
                new PropertyMetadata(null));
    }
}