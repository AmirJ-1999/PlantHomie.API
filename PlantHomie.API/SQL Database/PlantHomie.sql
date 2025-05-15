/*
   0)   Oprydning af eksisterende tabeller, hvis de findes
*/
IF OBJECT_ID('dbo.Notification', 'U') IS NOT NULL 
    DROP TABLE dbo.Notification;
IF OBJECT_ID('dbo.PlantLog', 'U') IS NOT NULL 
    DROP TABLE dbo.PlantLog;
IF OBJECT_ID('dbo.Plant', 'U') IS NOT NULL 
    DROP TABLE dbo.Plant;
IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL 
    DROP TABLE dbo.[User];
GO

/*
   0)   Opret selve databasen (spring over hvis den findes)
*/
IF DB_ID('PlantHomie') IS NULL
    CREATE DATABASE PlantHomie;
GO
USE PlantHomie;
GO

-- 1)   Brugere
CREATE TABLE dbo.[User] (
    User_ID        INT          IDENTITY(1,1) PRIMARY KEY,
    UserName       VARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash   VARCHAR(200) NOT NULL,
    Subscription   VARCHAR(20)  NOT NULL,     -- Free / Premium_…
    Plants_amount  INT          NULL          -- udfyldes i API'et
);
GO

-- 2)   Planter
CREATE TABLE dbo.Plant (
    Plant_ID     INT            IDENTITY(1,1) PRIMARY KEY,
    Plant_Name   VARCHAR(50),   -- Unik begrænsning fjernet, da planter nu er pr. bruger
    Plant_type   VARCHAR(50),
    ImageUrl     NVARCHAR(255)  NULL,
    User_ID      INT            NOT NULL,     -- User_ID tilføjet for at tilknytte planter til brugere
    CONSTRAINT FK_Plant_User FOREIGN KEY (User_ID)
        REFERENCES dbo.[User] (User_ID) ON DELETE CASCADE
);
GO

-- 3)   Plantelog (sensor-målinger)
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

-- 4)   Notifikationer
CREATE TABLE dbo.Notification (
    Notification_ID INT           IDENTITY(1,1) PRIMARY KEY,
    Dato_Tid        DATETIME      DEFAULT(GETUTCDATE()),
    Plant_Type      VARCHAR(50),
    Plant_ID        INT           NOT NULL,
    User_ID         INT           NOT NULL,
    CONSTRAINT FK_Notification_Plant FOREIGN KEY (Plant_ID)
        REFERENCES dbo.Plant (Plant_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Notification_User FOREIGN KEY (User_ID)
        REFERENCES dbo.[User] (User_ID) ON DELETE NO ACTION -- Ændret ON DELETE CASCADE til ON DELETE NO ACTION for at undgå flere kaskade stier
);
GO

-- 5)   (Valgfrit) Seed data til test
-- Opret en testbruger først (kræves for fremmednøglebegrænsning)
INSERT INTO dbo.[User] (UserName, PasswordHash, Subscription, Plants_amount)
VALUES (
    'dummyuser',
    'abc123hashedpassword',  -- Antag en hash; brug en rigtig hashing-funktion i praksis
    'Free',
    10
);
GO

-- Nu kan vi oprette planter der tilhører denne bruger
INSERT INTO dbo.Plant (Plant_Name, Plant_type, User_ID)
VALUES ('Test plant', 'Dummy', 1);

INSERT INTO dbo.Plant (Plant_Name, Plant_type, User_ID)
VALUES ('Demo Plant', 'Succulent', 1);
GO

-- Tilføj nogle plantelogge
INSERT INTO dbo.PlantLog (Plant_ID, TemperatureLevel, WaterLevel, AirHumidityLevel)
VALUES (1, 21.5, 45.0, 55.0);
GO

-- Tilføj en notifikation
INSERT INTO dbo.Notification (Plant_Type, Plant_ID, User_ID)
VALUES ('Dummy', 1, 1);
GO

/* Fjern for at droppe alle tabeller
-- DROP TABLE dbo.Notification;
-- DROP TABLE dbo.PlantLog;
-- DROP TABLE dbo.Plant;
-- DROP TABLE dbo.[User];
*/