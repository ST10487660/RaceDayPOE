/*
    RaceDay - Part 1 Database Script
    Target: Microsoft SQL Server / SSMS
    Purpose: Creates and seeds the RaceDay relational database.
*/

IF DB_ID(N'RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

-- Drop tables in dependency order so the script can be re-run during testing.
IF OBJECT_ID(N'dbo.Weather', N'U') IS NOT NULL DROP TABLE dbo.Weather;
IF OBJECT_ID(N'dbo.Results', N'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID(N'dbo.Enrolments', N'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID(N'dbo.EventCategories', N'U') IS NOT NULL DROP TABLE dbo.EventCategories;
IF OBJECT_ID(N'dbo.Routes', N'U') IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Events', N'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    UserID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Users PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL
        CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'Organiser', N'Participant')),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME()
);
GO

CREATE TABLE dbo.Events
(
    EventID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Events PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    MaxParticipants INT NOT NULL
        CONSTRAINT CK_Events_MaxParticipants CHECK (MaxParticipants > 0),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Events_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Events_Organiser
        FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID)
);
GO

CREATE TABLE dbo.Categories
(
    CategoryID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Categories PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL
        CONSTRAINT UQ_Categories_Name UNIQUE,
    Description NVARCHAR(300) NULL
);
GO

CREATE TABLE dbo.EventCategories
(
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,

    CONSTRAINT PK_EventCategories PRIMARY KEY (EventID, CategoryID),

    CONSTRAINT FK_EventCategories_Event
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID),

    CONSTRAINT FK_EventCategories_Category
        FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID)
);
GO

CREATE TABLE dbo.Routes
(
    RouteID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Routes PRIMARY KEY,
    EventID INT NOT NULL
        CONSTRAINT UQ_Routes_Event UNIQUE,
    RouteName NVARCHAR(150) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL
        CONSTRAINT CK_Routes_Distance CHECK (DistanceKm > 0),
    StartPoint NVARCHAR(200) NOT NULL,
    EndPoint NVARCHAR(200) NOT NULL,

    CONSTRAINT FK_Routes_Event
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

CREATE TABLE dbo.Enrolments
(
    EnrolmentID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Enrolments PRIMARY KEY,
    EventID INT NOT NULL,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2(0) NOT NULL
        CONSTRAINT DF_Enrolments_Date DEFAULT SYSDATETIME(),
    Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Enrolments_Status DEFAULT N'Confirmed'
        CONSTRAINT CK_Enrolments_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled')),

    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantID, EventID),

    CONSTRAINT FK_Enrolments_Event_Category
        FOREIGN KEY (EventID, CategoryID)
        REFERENCES dbo.EventCategories(EventID, CategoryID),

    CONSTRAINT FK_Enrolments_Participant
        FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID)
);
GO

CREATE TABLE dbo.Results
(
    ResultID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Results PRIMARY KEY,
    EnrolmentID INT NOT NULL
        CONSTRAINT UQ_Results_Enrolment UNIQUE,
    Position INT NULL
        CONSTRAINT CK_Results_Position CHECK (Position IS NULL OR Position > 0),
    FinishTime TIME(0) NULL,
    DistanceKm DECIMAL(6,2) NULL
        CONSTRAINT CK_Results_Distance CHECK (DistanceKm IS NULL OR DistanceKm > 0),
    RecordedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Results_RecordedAt DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Results_Enrolment
        FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID)
);
GO

CREATE TABLE dbo.Weather
(
    WeatherID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Weather PRIMARY KEY,
    EventID INT NOT NULL,
    RecordedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Weather_RecordedAt DEFAULT SYSDATETIME(),
    TemperatureC DECIMAL(5,2) NULL,
    Conditions NVARCHAR(100) NULL,
    WindSpeedKph DECIMAL(6,2) NULL
        CONSTRAINT CK_Weather_Wind CHECK (WindSpeedKph IS NULL OR WindSpeedKph >= 0),

    CONSTRAINT FK_Weather_Event
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

-- Seed users: at least two organisers and two participants.
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
VALUES
(N'Naledi Mokoena', N'naledi.organiser@raceday.co.za', N'PART1-DEMO-HASH-001', N'Organiser'),
(N'Thabo Dlamini', N'thabo.organiser@raceday.co.za', N'PART1-DEMO-HASH-002', N'Organiser'),
(N'Lerato Molefe', N'lerato.participant@raceday.co.za', N'PART1-DEMO-HASH-003', N'Participant'),
(N'Kabelo Ndlovu', N'kabelo.participant@raceday.co.za', N'PART1-DEMO-HASH-004', N'Participant');
GO

INSERT INTO dbo.Categories (CategoryName, Description)
VALUES
(N'10 km Run', N'Ten kilometre road running category.'),
(N'21 km Half Marathon', N'Half marathon road running category.'),
(N'5 km Community Walk', N'Five kilometre community walking category.'),
(N'40 km Cycle', N'Forty kilometre community cycling category.');
GO

INSERT INTO dbo.Events
    (OrganiserID, EventName, Description, EventDate, Location, MaxParticipants)
VALUES
(1, N'Johannesburg Spring Run', N'Community road-running event in Johannesburg.', '2026-10-18', N'Johannesburg, Gauteng', 1500),
(1, N'Pretoria City Half Marathon', N'Half-marathon event through Pretoria.', '2026-11-08', N'Pretoria, Gauteng', 2000),
(2, N'Soweto Community Cycle', N'Community cycling event supporting local charities.', '2026-11-22', N'Soweto, Gauteng', 1000);
GO

-- Link categories to each event.
INSERT INTO dbo.EventCategories (EventID, CategoryID)
VALUES
(1, 1), (1, 3),
(2, 2), (2, 1),
(3, 4);
GO

INSERT INTO dbo.Routes
    (EventID, RouteName, DistanceKm, StartPoint, EndPoint)
VALUES
(1, N'Johannesburg Spring Loop', 10.00, N'Emmarentia Dam', N'Emmarentia Dam'),
(2, N'Pretoria City Route', 21.10, N'Union Buildings', N'Union Buildings'),
(3, N'Soweto Charity Loop', 40.00, N'Orlando Stadium', N'Orlando Stadium');
GO

-- Sample participant enrolments.
INSERT INTO dbo.Enrolments
    (EventID, ParticipantID, CategoryID, Status)
VALUES
(1, 3, 1, N'Confirmed'),
(1, 4, 3, N'Confirmed'),
(2, 3, 2, N'Confirmed'),
(3, 4, 4, N'Confirmed');
GO

-- Sample results for completed
INSERT INTO dbo.Results
    (EnrolmentID, Position, FinishTime, DistanceKm)
VALUES
(1, 18, '00:52:14', 10.00),
(2, 7, '00:48:31', 5.00);
GO

-- Sample weather records for event preparation
INSERT INTO dbo.Weather
    (EventID, TemperatureC, Conditions, WindSpeedKph)
VALUES
(1, 21.50, N'Partly cloudy', 14.20),
(2, 19.00, N'Sunny', 11.80),
(3, 23.20, N'Clear', 9.50);
GO

-- Verification queries.
SELECT * FROM dbo.Users;
SELECT * FROM dbo.Events;
SELECT * FROM dbo.Categories;
SELECT * FROM dbo.EventCategories;
SELECT * FROM dbo.Routes;
SELECT * FROM dbo.Enrolments;
SELECT * FROM dbo.Results;
SELECT * FROM dbo.Weather;
GO
