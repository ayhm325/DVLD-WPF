USE [DVLD];
GO

IF DB_NAME() <> N'DVLD'
    THROW 51000, 'Wrong database. Migration aborted.', 1;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [TestAppointments] ADD [CancelledByUserID] int NULL;
GO

ALTER TABLE [TestAppointments] ADD [CancelledAt] datetime2 NULL;
GO

ALTER TABLE [TestAppointments] ADD [NoShowDate] datetime2 NULL;
GO

ALTER TABLE [TestAppointments] ADD [Status] int NULL;
GO

CREATE INDEX [IX_TestAppointments_CancelledByUserID] ON [TestAppointments] ([CancelledByUserID]);
GO

CREATE INDEX [IX_TestAppointments_Status] ON [TestAppointments] ([Status]);
GO

ALTER TABLE [TestAppointments] ADD CONSTRAINT [FK_TestAppointments_Users_CancelledByUserID] FOREIGN KEY ([CancelledByUserID]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION;
GO

UPDATE TestAppointments
                  SET Status = 1
                  WHERE IsLocked = 0
                    AND NOT EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);
GO

UPDATE TestAppointments
                  SET Status = 2
                  WHERE IsLocked = 1
                    AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);
GO

UPDATE TestAppointments
                  SET Status = 2,
                      IsLocked = 1
                  WHERE IsLocked = 0
                    AND EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID);
GO

UPDATE TestAppointments
                  SET Status = 3,
                      NoShowDate = DATEADD(minute, 40, AppointmentDate)
                  WHERE IsLocked = 1
                    AND NOT EXISTS (SELECT 1 FROM Tests t WHERE t.TestAppointmentID = TestAppointments.TestAppointmentID)
                    AND DATEADD(minute, 40, AppointmentDate) <= CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Jordan Standard Time' AS datetime2);
GO

ALTER TABLE TestAppointments ADD CONSTRAINT DF_TestAppointments_Status DEFAULT 1 FOR Status;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261102232837_AddAppointmentLifecycle', N'8.0.18');
GO

COMMIT;
GO

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

