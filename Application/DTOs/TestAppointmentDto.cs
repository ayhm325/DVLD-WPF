


using Domain.Enums;

namespace Application.DTOs
{
    public class TestAppointmentDto
    {
        public int AppointmentID { get; set; }

        public int TestAppointmentID { get; set; }

        public int TestTypeID { get; set; }

        public TestResultType TestResult { get; set; }

        public string CreatedByUserName { get; set; } = string.Empty;

        public string TestTypeName { get; set; } = string.Empty;

        public int LocalDrivingLicenseApplicationID { get; set; }

        public DateTime AppointmentDate { get; set; }

        public decimal PaidFees { get; set; }

        public int CreatedByUserID { get; set; }

        public bool IsLocked { get; set; }

        public int? RetakeTestApplicationID { get; set; }

        // UI Helpers
        public string TestResultText => TestResult.ToString();

        public string Status => IsLocked ? "Locked" : "Open";

        public string AppointmentDateFormatted =>
            AppointmentDate.ToString("yyyy-MM-dd HH:mm");


    }
}


//using CommunityToolkit.Mvvm.ComponentModel;
//using Domain.Enums;


//namespace Application.DTOs
//{
//    public partial class TestAppointmentDto : ObservableObject
//    {
//        [ObservableProperty]
//        private int _appointmentID;

//        [ObservableProperty]
//        private int _testAppointmentID;

//        [ObservableProperty]
//        private int _testTypeID;

//        [ObservableProperty]
//        private TestResultType _testResult;

//        [ObservableProperty]
//        private string _createdByUserName = string.Empty;

//        [ObservableProperty]
//        private string _testTypeName = string.Empty;

//        [ObservableProperty]
//        private int _localDrivingLicenseApplicationID;

//        [ObservableProperty]
//        private DateTime _appointmentDate;

//        [ObservableProperty]
//        private decimal _paidFees;

//        [ObservableProperty]
//        private int _createdByUserID;       

//        [ObservableProperty]
//        private bool _isLocked;

//        [ObservableProperty]
//        private int? _retakeTestApplicationID;

//        // =========================
//        // READ-ONLY UI PROPERTIES
//        // يتم تحديثها تلقائياً عند تغيير الخصائص المرتبطة بها
//        // =========================
//        public string Status => IsLocked ? "Locked" : "Open";

//        public string TestResultText => TestResult.ToString();

//        public string AppointmentDateFormatted => AppointmentDate.ToString("yyyy-MM-dd HH:mm");

//        // هذا الجزء اختياري: لإجبار الواجهة على تحديث الـ Status عند تغيير IsLocked
//        partial void OnIsLockedChanged(bool value) => OnPropertyChanged(nameof(Status));

//        // لإجبار الواجهة على تحديث التنسيق عند تغيير التاريخ
//        partial void OnAppointmentDateChanged(DateTime value) => OnPropertyChanged(nameof(AppointmentDateFormatted));
//    }
//}

