using DVLD.Contracts.Person;
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

        public PersonResponse? Person
        {
            get => (PersonResponse?)GetValue(PersonProperty);
            set => SetValue(PersonProperty, value);
        }

        public static readonly DependencyProperty PersonProperty =
            DependencyProperty.Register(
                nameof(Person),
                typeof(PersonResponse),
                typeof(InformationPerson),
                new PropertyMetadata(null));
    }
}