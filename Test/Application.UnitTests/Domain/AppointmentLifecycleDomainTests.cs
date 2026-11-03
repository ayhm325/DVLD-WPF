using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.UnitTests.Lifecycle;

public sealed class AppointmentLifecycleDomainTests
{
    [Fact]
    public void AppointmentStatus_EnumValues_MatchApprovedSpecification()
    {
        Assert.Equal(1, (int)AppointmentStatus.Scheduled);
        Assert.Equal(2, (int)AppointmentStatus.Completed);
        Assert.Equal(3, (int)AppointmentStatus.NoShow);
        Assert.Equal(4, (int)AppointmentStatus.Cancelled);
    }

    [Fact]
    public void TestAppointment_InitialState_DefaultsToScheduledWithNullMetadata()
    {
        var appointment = new TestAppointment();

        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.False(appointment.IsLocked);
        Assert.Null(appointment.CancelledAt);
        Assert.Null(appointment.CancelledByUserID);
        Assert.Null(appointment.CancelledByUser);
        Assert.Null(appointment.NoShowDate);
        Assert.Null(appointment.Test);
    }

    [Fact]
    public void TestAppointment_CanSetCompletedStatus_PreservingTestEntity()
    {
        var appointment = new TestAppointment
        {
            TestAppointmentID = 10,
            Status = AppointmentStatus.Completed,
            IsLocked = true,
            Test = new Test
            {
                TestID = 100,
                TestResult = true,
                CreatedByUserID = 5,
                Notes = "Passed cleanly"
            }
        };

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.True(appointment.IsLocked);
        Assert.NotNull(appointment.Test);
        Assert.True(appointment.Test.TestResult);
        Assert.Equal(5, appointment.Test.CreatedByUserID);
    }

    [Fact]
    public void TestAppointment_CanSetNoShowStatus_WithoutFakeTestRecord()
    {
        var now = DateTime.UtcNow;
        var appointment = new TestAppointment
        {
            TestAppointmentID = 11,
            Status = AppointmentStatus.NoShow,
            IsLocked = true,
            NoShowDate = now,
            Test = null // Explicitly null: No fake test record
        };

        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
        Assert.True(appointment.IsLocked);
        Assert.Equal(now, appointment.NoShowDate);
        Assert.Null(appointment.Test);
    }

    [Fact]
    public void TestAppointment_CanSetCancelledStatus_WithCancellationMetadata()
    {
        var now = DateTime.UtcNow;
        var appointment = new TestAppointment
        {
            TestAppointmentID = 12,
            Status = AppointmentStatus.Cancelled,
            IsLocked = true,
            CancelledAt = now,
            CancelledByUserID = 42
        };

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.True(appointment.IsLocked);
        Assert.Equal(now, appointment.CancelledAt);
        Assert.Equal(42, appointment.CancelledByUserID);
        Assert.Null(appointment.Test);
    }
}
