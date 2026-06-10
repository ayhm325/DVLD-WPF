using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Presentation.Views.Controls
{
    public partial class ApplicationBasicInfo : UserControl
    {
        public ApplicationBasicInfo()
        {
            InitializeComponent();
        }

        public int ApplicationId
        {
            get => (int)GetValue(ApplicationIdProperty);
            set => SetValue(ApplicationIdProperty, value);
        }

        public static readonly DependencyProperty ApplicationIdProperty =
            DependencyProperty.Register(
                nameof(ApplicationId),
                typeof(int),
                typeof(ApplicationBasicInfo));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(string),
                typeof(ApplicationBasicInfo));

        public decimal Fees
        {
            get => (decimal)GetValue(FeesProperty);
            set => SetValue(FeesProperty, value);
        }

        public static readonly DependencyProperty FeesProperty =
            DependencyProperty.Register(
                nameof(Fees),
                typeof(decimal),
                typeof(ApplicationBasicInfo));

        public string Type
        {
            get => (string)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
                nameof(Type),
                typeof(string),
                typeof(ApplicationBasicInfo));

        public string Applicant
        {
            get => (string)GetValue(ApplicantProperty);
            set => SetValue(ApplicantProperty, value);
        }

        public static readonly DependencyProperty ApplicantProperty =
            DependencyProperty.Register(
                nameof(Applicant),
                typeof(string),
                typeof(ApplicationBasicInfo));

        public DateTime ApplicationDate
        {
            get => (DateTime)GetValue(ApplicationDateProperty);
            set => SetValue(ApplicationDateProperty, value);
        }

        public static readonly DependencyProperty ApplicationDateProperty =
            DependencyProperty.Register(
                nameof(ApplicationDate),
                typeof(DateTime),
                typeof(ApplicationBasicInfo));

        public DateTime StatusDate
        {
            get => (DateTime)GetValue(StatusDateProperty);
            set => SetValue(StatusDateProperty, value);
        }

        public static readonly DependencyProperty StatusDateProperty =
            DependencyProperty.Register(
                nameof(StatusDate),
                typeof(DateTime),
                typeof(ApplicationBasicInfo));

        public string CreatedBy
        {
            get => (string)GetValue(CreatedByProperty);
            set => SetValue(CreatedByProperty, value);
        }

        public static readonly DependencyProperty CreatedByProperty =
            DependencyProperty.Register(
                nameof(CreatedBy),
                typeof(string),
                typeof(ApplicationBasicInfo));

        public ICommand ViewPersonInfoCommand
        {
            get => (ICommand)GetValue(ViewPersonInfoCommandProperty);
            set => SetValue(ViewPersonInfoCommandProperty, value);
        }

        public static readonly DependencyProperty ViewPersonInfoCommandProperty =
            DependencyProperty.Register(
                nameof(ViewPersonInfoCommand),
                typeof(ICommand),
                typeof(ApplicationBasicInfo));
    }
}