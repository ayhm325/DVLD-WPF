using Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserSessionRepository(DVLDDbContext context)
    : Application.Interfaces.IUserSessionRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    public Task<UserSession?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        _context.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                session => session.RefreshTokenHash == tokenHash,
                cancellationToken);

    public async Task<bool> TryConsumeAsync(
        Guid sessionId,
        DateTime nowUtc,
        Guid replacedBySessionId,
        CancellationToken cancellationToken = default)
    {
        // The conditional UPDATE is the concurrency arbiter: a second caller blocks
        // on the row lock and then observes RevokedAtUtc != null, affecting 0 rows.
        var affected = await _context.UserSessions
            .Where(session =>
                session.Id == sessionId &&
                session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        session => session.RevokedAtUtc,
                        nowUtc)
                    .SetProperty(
                        session => session.RevocationReason,
                        SessionRevocationReasons.Rotated)
                    .SetProperty(
                        session => session.ReplacedBySessionId,
                        replacedBySessionId),
                cancellationToken);

        return affected > 0;
    }

    public async Task AddAsync(
        UserSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await _context.UserSessions.AddAsync(session, cancellationToken);
    }

    public Task<int> RevokeFamilyAsync(
        Guid tokenFamilyId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default) =>
        _context.UserSessions
            .Where(session =>
                session.TokenFamilyId == tokenFamilyId &&
                session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        session => session.RevokedAtUtc,
                        nowUtc)
                    .SetProperty(
                        session => session.RevocationReason,
                        reason),
                cancellationToken);

    public Task<int> RevokeAllForUserAsync(
        int userId,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default) =>
        _context.UserSessions
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        session => session.RevokedAtUtc,
                        nowUtc)
                    .SetProperty(
                        session => session.RevocationReason,
                        reason),
                cancellationToken);

    public Task<bool> HasActiveSessionInFamilyAsync(
        Guid tokenFamilyId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default) =>
        _context.UserSessions
            .AsNoTracking()
            .AnyAsync(
                session =>
                    session.TokenFamilyId == tokenFamilyId &&
                    session.RevokedAtUtc == null &&
                    session.ExpiresAtUtc > nowUtc,
                cancellationToken);

    public Task<int> RevokeByTokenHashAsync(
        string tokenHash,
        DateTime nowUtc,
        string reason,
        CancellationToken cancellationToken = default) =>
        _context.UserSessions
            .Where(session =>
                session.RefreshTokenHash == tokenHash &&
                session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        session => session.RevokedAtUtc,
                        nowUtc)
                    .SetProperty(
                        session => session.RevocationReason,
                        reason),
                cancellationToken);
}
