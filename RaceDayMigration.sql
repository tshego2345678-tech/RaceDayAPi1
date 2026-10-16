BEGIN TRANSACTION;
GO

CREATE TABLE [Organiser] (
    [OrganiserId] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [OrganisationName] nvarchar(150) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Organiser] PRIMARY KEY ([OrganiserId])
);
GO

CREATE TABLE [Participantsss] (
    [ParticipantId] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [DateOfBirth] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Participantsss] PRIMARY KEY ([ParticipantId])
);
GO

CREATE TABLE [Events] (
    [EventId] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [EventDate] datetime2 NOT NULL,
    [Location] nvarchar(150) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [OrganiserId] int NOT NULL,
    CONSTRAINT [PK_Events] PRIMARY KEY ([EventId]),
    CONSTRAINT [FK_Events_Organiser_OrganiserId] FOREIGN KEY ([OrganiserId]) REFERENCES [Organiser] ([OrganiserId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [DistanceKm] decimal(5,2) NOT NULL,
    [EntryFee] decimal(8,2) NOT NULL,
    [MaxParticipants] int NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [FK_Categories_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Enrolments] (
    [EnrolmentId] int NOT NULL IDENTITY,
    [ParticipantId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [EnrolmentDate] datetime2 NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_Enrolments] PRIMARY KEY ([EnrolmentId]),
    CONSTRAINT [FK_Enrolments_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Enrolments_Participantsss_ParticipantId] FOREIGN KEY ([ParticipantId]) REFERENCES [Participantsss] ([ParticipantId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Results] (
    [ResultId] int NOT NULL IDENTITY,
    [EnrolmentId] int NOT NULL,
    [FinishTime] time NULL,
    [Position] int NULL,
    [Status] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_Results] PRIMARY KEY ([ResultId]),
    CONSTRAINT [FK_Results_Enrolments_EnrolmentId] FOREIGN KEY ([EnrolmentId]) REFERENCES [Enrolments] ([EnrolmentId]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Categories_EventId] ON [Categories] ([EventId]);
GO

CREATE INDEX [IX_Enrolments_CategoryId] ON [Enrolments] ([CategoryId]);
GO

CREATE INDEX [IX_Enrolments_ParticipantId] ON [Enrolments] ([ParticipantId]);
GO

CREATE INDEX [IX_Events_OrganiserId] ON [Events] ([OrganiserId]);
GO

CREATE UNIQUE INDEX [IX_Results_EnrolmentId] ON [Results] ([EnrolmentId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011173306_AddRaceDayModels', N'8.0.31');
GO

COMMIT;
GO

