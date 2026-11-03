using Application.Common.Results;
using Application.Validators;
using Xunit;

namespace Application.UnitTests.Validators;

public sealed class AppointmentCutoffValidatorTests
{
    [Fact]
    public void Cutoff_ExactlyTwoCalendarDaysBefore_IsAllowed()
    {
        // Appointment: Nov 3 at 08:00
        var appointmentDate = new DateTime(2026, 11, 3, 8, 0, 0);
        // Current Jordan Date: Nov 1 at 12:00 (Nov 3 - Nov 1 = 2 calendar days)
        var currentJordanTime = new DateTime(2026, 11, 1, 12, 0, 0);

        var result = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(appointmentDate, currentJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Cutoff_OneCalendarDayBefore_IsRejected()
    {
        // Appointment: Nov 3 at 08:00
        var appointmentDate = new DateTime(2026, 11, 3, 8, 0, 0);
        // Current Jordan Date: Nov 2 at 10:00 (Nov 3 - Nov 2 = 1 calendar day)
        var currentJordanTime = new DateTime(2026, 11, 2, 10, 0, 0);

        var result = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(appointmentDate, currentJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Appointments cannot be modified or cancelled less than two calendar days before the scheduled date.",
            result.Error);
    }

    [Fact]
    public void Cutoff_SameDay_IsRejected()
    {
        // Appointment: Nov 3 at 14:00
        var appointmentDate = new DateTime(2026, 11, 3, 14, 0, 0);
        // Current Jordan Date: Nov 3 at 08:00 (Nov 3 - Nov 3 = 0 calendar days)
        var currentJordanTime = new DateTime(2026, 11, 3, 8, 0, 0);

        var result = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(appointmentDate, currentJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Cutoff_MoreThanTwoCalendarDaysBefore_IsAllowed()
    {
        // Appointment: Nov 5 at 08:00
        var appointmentDate = new DateTime(2026, 11, 5, 8, 0, 0);
        // Current Jordan Date: Nov 1 at 09:00 (Nov 5 - Nov 1 = 4 calendar days)
        var currentJordanTime = new DateTime(2026, 11, 1, 9, 0, 0);

        var result = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(appointmentDate, currentJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Cutoff_JordanMidnightBoundary_AllowedJustBeforeMidnight_RejectedJustAfter()
    {
        // Appointment: Nov 3 at 08:00
        var appointmentDate = new DateTime(2026, 11, 3, 8, 0, 0);

        // One second before midnight on Nov 1 (23:59:59): Calendar date is Nov 1 -> Difference is 2 days -> Allowed!
        var justBeforeMidnight = new DateTime(2026, 11, 1, 23, 59, 59);
        var resultBefore = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(appointmentDate, justBeforeMidnight);
        Assert.True(resultBefore.IsSuccess);

        // Midnight on Nov 2 (00:00:00): Calendar date is Nov 2 -> Difference is 1 day -> Rejected!
        var atMidnight = new DateTime(2026, 11, 2, 0, 0, 0);
        var resultAfter = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(appointmentDate, atMidnight);
        Assert.True(resultAfter.IsFailure);
        Assert.Equal(ErrorType.Validation, resultAfter.ErrorType);
    }

    [Fact]
    public void Cutoff_PastAppointmentDate_IsRejected()
    {
        // Appointment was yesterday
        var appointmentDate = new DateTime(2026, 11, 1, 8, 0, 0);
        var currentJordanTime = new DateTime(2026, 11, 2, 8, 0, 0);

        var result = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(appointmentDate, currentJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
