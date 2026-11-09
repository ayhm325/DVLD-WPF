using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Persistence port for refresh-token generations. All mutations that enforce the
/// single-winner rotation guarantee are executed as conditional database updates.
/// </summary>
public interface IUserSessionRepository
{
    Task<UserSession?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically consumes an active, unexpired session. Returns true only for the
    /// single caller whose conditional update affected a row.
    /// </summary>
    Task<bool> TryConsumeAsync(
        Guid sessionId,
        DateTime nowUtc,
        Guid replacedBySessionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserSession session,
        CancellationToken cancellationToken = default);

    Task<int> RevokeFamilyAsync(
        Guid tokenFamilyId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default);

    Task<int> RevokeAllForUserAsync(
        int userId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveSessionInFamilyAsync(
        Guid tokenFamilyId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes the active session that owns the supplied token hash (logout).</summary>
    Task<int> RevokeByTokenHashAsync(
        string tokenHash,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default);
}
