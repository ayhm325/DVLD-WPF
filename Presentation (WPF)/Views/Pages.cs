using Presentation.ViewModels;
using System.Windows.Controls;

namespace Presentation.Views
{
    internal class Pages
    {
        internal class AddEditPersonPage : Page
        {
            private PersonViewModel addEditVm;

            public AddEditPersonPage(PersonViewModel addEditVm)
            {
                this.addEditVm = addEditVm;
            }
        }
    }
}