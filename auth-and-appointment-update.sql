BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [SecurityStamp] nvarchar(64) NOT NULL DEFAULT (CONVERT(nvarchar(64), NEWID()));
GO

CREATE TABLE [UserSessions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] int NOT NULL,
    [RefreshTokenHash] nvarchar(64) NOT NULL,
    [TokenFamilyId] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [LastUsedAtUtc] datetime2 NOT NULL,
    [RevokedAtUtc] datetime2 NULL,
    [RevocationReason] nvarchar(64) NULL,
    [ReplacedBySessionId] uniqueidentifier NULL,
    CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_UserSessions_RefreshTokenHash] ON [UserSessions] ([RefreshTokenHash]);
GO

CREATE INDEX [IX_UserSessions_TokenFamilyId] ON [UserSessions] ([TokenFamilyId]);
GO

CREATE INDEX [IX_UserSessions_UserId_RevokedAtUtc] ON [UserSessions] ([UserId], [RevokedAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261109163257_AuthSessions', N'8.0.18');
GO

COMMIT;
GO

