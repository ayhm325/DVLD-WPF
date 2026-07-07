





using CommunityToolkit.Mvvm.ComponentModel;

namespace Application.DTOs
{
    public partial class ScheduleTestDto : ObservableObject
    {
        [ObservableProperty] private int _appointmentID;
        [ObservableProperty] private int _retakeTestApplicationID;
        [ObservableProperty] private int _localDrivingLicenseApplicationID;
        [ObservableProperty] private string? _licenseClassName;
        [ObservableProperty] private string? _fullName;
        [ObservableProperty] private int _trial;
        [ObservableProperty] private DateTime _date;
        [ObservableProperty] private decimal _fees;
        [ObservableProperty] private int _testTypeID;
        [ObservableProperty] private decimal _retakerFees;
        [ObservableProperty] private int _testID;
        [ObservableProperty] private bool _result;
        [ObservableProperty] private string? _notes;
    }
}
