-- PlantHomieNEW.sql - Complete Database Setup Script
-- This script will create a new PlantHomie database from scratch

-- Create a new database (uncomment if you want to create a new database)
-- CREATE DATABASE PlantHomie;
-- GO
-- USE PlantHomie;
-- GO

-- Drop existing tables if they exist
IF OBJECT_ID('dbo.Notification', 'U') IS NOT NULL DROP TABLE dbo.Notification;
IF OBJECT_ID('dbo.PlantLog', 'U') IS NOT NULL DROP TABLE dbo.PlantLog;
IF OBJECT_ID('dbo.Plant', 'U') IS NOT NULL DROP TABLE dbo.Plant;
IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL DROP TABLE dbo.[User];
GO

-- 1) Users Table
CREATE TABLE dbo.[User] (
    User_ID        INT          IDENTITY(1,1) PRIMARY KEY,
    UserName       VARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash   VARCHAR(200) NOT NULL,
    Subscription   VARCHAR(20)  NOT NULL,  -- Free / Premium_…
    Plants_amount  INT          NULL,
    AutoMode       BIT          NOT NULL DEFAULT 1
);
GO

-- 2) Plants Table
CREATE TABLE dbo.Plant (
    Plant_ID     INT            IDENTITY(1,1) PRIMARY KEY,
    Plant_Name   VARCHAR(50)    NOT NULL,
    Plant_type   VARCHAR(50),
    ImageUrl     NVARCHAR(255)  NULL,
    User_ID      INT            NOT NULL,
    CONSTRAINT FK_Plant_User FOREIGN KEY (User_ID)
        REFERENCES dbo.[User] (User_ID) ON DELETE CASCADE
);
GO

-- Add unique constraint for plant name per user
ALTER TABLE dbo.Plant
ADD CONSTRAINT UQ_Plant_Name_User UNIQUE (Plant_Name, User_ID);
GO

-- 3) Plant Log Table (sensor measurements)
CREATE TABLE dbo.PlantLog (
    PlantLog_ID       INT IDENTITY(1,1) PRIMARY KEY,
    Plant_ID          INT          NOT NULL,
    Dato_Tid          DATETIME     DEFAULT(GETUTCDATE()),
    TemperatureLevel  FLOAT        NULL,
    LightLevel        FLOAT        NULL,
    WaterLevel        FLOAT        NULL,
    AirHumidityLevel  FLOAT        NULL,
    CONSTRAINT FK_PlantLog_Plant FOREIGN KEY (Plant_ID)
        REFERENCES dbo.Plant (Plant_ID) ON DELETE CASCADE
);
GO

-- 4) Notifications Table
CREATE TABLE dbo.Notification (
    Notification_ID   INT          IDENTITY(1,1) PRIMARY KEY,
    Dato_Tid          DATETIME     DEFAULT(GETUTCDATE()),
    Plant_Type        VARCHAR(50),
    Plant_ID          INT          NOT NULL,
    User_ID           INT          NOT NULL,
    Message           NVARCHAR(250) NULL,
    IsRead            BIT          NOT NULL DEFAULT 0,
    NotificationType  VARCHAR(50)  NULL DEFAULT 'System',
    CONSTRAINT FK_Notification_Plant FOREIGN KEY (Plant_ID)
        REFERENCES dbo.Plant (Plant_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Notification_User  FOREIGN KEY (User_ID)
        REFERENCES dbo.[User] (User_ID) ON DELETE NO ACTION
);
GO

-- 5) Insert sample data for testing

-- Insert test users
INSERT INTO dbo.[User] (UserName, PasswordHash, Subscription, Plants_amount, AutoMode)
VALUES ('dummyuser', 'abc123hashedpassword', 'Free', 10, 1);

INSERT INTO dbo.[User] (UserName, PasswordHash, Subscription, Plants_amount, AutoMode)
VALUES ('admin', 'hashed_admin_password', 'Premium', 25, 1);

-- Insert sample plants
INSERT INTO dbo.Plant (Plant_Name, Plant_type, ImageUrl, User_ID)
VALUES ('Test Plant', 'Dummy', 'https://example.com/images/testplant.jpg', 1);

INSERT INTO dbo.Plant (Plant_Name, Plant_type, ImageUrl, User_ID)
VALUES ('Demo Plant', 'Succulent', 'https://example.com/images/succulent.jpg', 1);

INSERT INTO dbo.Plant (Plant_Name, Plant_type, ImageUrl, User_ID)
VALUES ('Aloe Vera', 'Succulent', 'https://example.com/images/aloevera.jpg', 2);

INSERT INTO dbo.Plant (Plant_Name, Plant_type, ImageUrl, User_ID)
VALUES ('Monstera', 'Tropical', 'https://example.com/images/monstera.jpg', 2);

-- Insert sample plant logs
INSERT INTO dbo.PlantLog (Plant_ID, Dato_Tid, TemperatureLevel, LightLevel, WaterLevel, AirHumidityLevel)
VALUES (1, DATEADD(hour, -1, GETUTCDATE()), 21.5, 60.0, 45.0, 55.0);

INSERT INTO dbo.PlantLog (Plant_ID, Dato_Tid, TemperatureLevel, LightLevel, WaterLevel, AirHumidityLevel)
VALUES (1, GETUTCDATE(), 22.0, 65.0, 40.0, 52.0);

INSERT INTO dbo.PlantLog (Plant_ID, Dato_Tid, TemperatureLevel, LightLevel, WaterLevel, AirHumidityLevel)
VALUES (2, DATEADD(hour, -2, GETUTCDATE()), 19.5, 70.0, 30.0, 48.0);

INSERT INTO dbo.PlantLog (Plant_ID, Dato_Tid, TemperatureLevel, LightLevel, WaterLevel, AirHumidityLevel)
VALUES (3, DATEADD(hour, -3, GETUTCDATE()), 23.0, 80.0, 25.0, 45.0);

-- Insert sample notifications
INSERT INTO dbo.Notification (Dato_Tid, Plant_Type, Plant_ID, User_ID, Message, IsRead, NotificationType)
VALUES (DATEADD(hour, -5, GETUTCDATE()), 'Dummy', 1, 1, 'Your Test Plant needs water!', 0, 'Warning');

INSERT INTO dbo.Notification (Dato_Tid, Plant_Type, Plant_ID, User_ID, Message, IsRead, NotificationType)
VALUES (DATEADD(hour, -2, GETUTCDATE()), 'Succulent', 2, 1, 'Your Demo Plant is in critical condition!', 0, 'Critical');

INSERT INTO dbo.Notification (Dato_Tid, Plant_Type, Plant_ID, User_ID, Message, IsRead, NotificationType)
VALUES (DATEADD(hour, -1, GETUTCDATE()), 'Succulent', 3, 2, 'Your Aloe Vera needs more light!', 1, 'Warning');

PRINT 'PlantHomie database has been successfully created and populated with sample data.';
GO

-- HOW TO USE THIS SCRIPT:
-- 1. Open SQL Server Management Studio
-- 2. Connect to your database server
-- 3. If you want to create a new database, uncomment the CREATE DATABASE line at the top
-- 4. Execute the entire script
-- 5. The database will be created with all tables and sample data
