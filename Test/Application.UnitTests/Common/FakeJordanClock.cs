using Application.Common.Time;
using Application.Interfaces;

namespace Application.UnitTests.Common;

/// <summary>
/// Deterministic in-memory implementation of <see cref="IJordanClock"/> for unit tests.
/// Keeps <see cref="Now"/>, <see cref="Today"/>, and <see cref="UtcNow"/> strictly synchronized
/// using <see cref="JordanTimeZoneProvider"/>.
/// </summary>
public sealed class FakeJordanClock : IJordanClock
{
    public static readonly DateTime DefaultJordanNow =
        new(2026, 11, 3, 10, 0, 0, DateTimeKind.Unspecified);

    private DateTime _utcNow;

    public FakeJordanClock()
        : this(DefaultJordanNow)
    {
    }

    public FakeJordanClock(DateTime jordanLocalNow)
    {
        SetNow(jordanLocalNow);
    }

    public static FakeJordanClock FromUtc(DateTime utcNow)
    {
        var clock = new FakeJordanClock();
        clock.SetUtcNow(utcNow);
        return clock;
    }

    public static FakeJordanClock FromJordanLocal(DateTime jordanLocalNow) =>
        new(jordanLocalNow);

    public DateTime UtcNow => _utcNow;

    public DateTime Now =>
        JordanTimeZoneProvider.ConvertUtcToJordan(_utcNow);

    public DateOnly Today =>
        DateOnly.FromDateTime(Now);

    public void SetNow(DateTime jordanLocalNow)
    {
        var unspecified = DateTime.SpecifyKind(jordanLocalNow, DateTimeKind.Unspecified);
        _utcNow = JordanTimeZoneProvider.ConvertJordanToUtc(unspecified);
    }

    public void SetUtcNow(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must have DateTimeKind.Utc.", nameof(utcNow));
        }

        _utcNow = utcNow;
    }

    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }
}
