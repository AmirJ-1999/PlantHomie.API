/*
   0)   Rydder op i tabeller, hvis de nu skulle eksistere
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
   0)   Opretter databasen, hvis den ikke allerede er der
*/
IF DB_ID('PlantHomie') IS NULL
    CREATE DATABASE PlantHomie;
GO
USE PlantHomie;
GO

-- 1)   Tabel til brugerne
CREATE TABLE dbo.[User] (
    User_ID        INT          IDENTITY(1,1) PRIMARY KEY,
    UserName       VARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash   VARCHAR(200) NOT NULL,
    Subscription   VARCHAR(20)  NOT NULL,     -- Fx Free / Premium_...
    Plants_amount  INT          NULL          -- Opdateres via API'et
);
GO

-- 2)   Tabel til planterne
CREATE TABLE dbo.Plant (
    Plant_ID     INT            IDENTITY(1,1) PRIMARY KEY,
    Plant_Name   VARCHAR(50),   -- Navne er unikke pr. bruger, ikke globalt
    Plant_type   VARCHAR(50),
    ImageUrl     NVARCHAR(255)  NULL,
    User_ID      INT            NOT NULL,     -- Knytter planten til en specifik bruger
    CONSTRAINT FK_Plant_User FOREIGN KEY (User_ID)
        REFERENCES dbo.[User] (User_ID) ON DELETE CASCADE
);
GO

-- 3)   Tabel for plantelog (sensordata)
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

-- 4)   Tabel for notifikationer
CREATE TABLE dbo.Notification (
    Notification_ID INT           IDENTITY(1,1) PRIMARY KEY,
    Dato_Tid        DATETIME      DEFAULT(GETUTCDATE()),
    Plant_Type      VARCHAR(50),
    Plant_ID        INT           NOT NULL,
    User_ID         INT           NOT NULL,
    CONSTRAINT FK_Notification_Plant FOREIGN KEY (Plant_ID)
        REFERENCES dbo.Plant (Plant_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Notification_User FOREIGN KEY (User_ID)
        REFERENCES dbo.[User] (User_ID) ON DELETE NO ACTION -- Undgår 'multiple cascade paths' ved sletning
);
GO

-- 5)   (Valgfrit) Lidt testdata, hvis man har lyst
-- Først en testbruger (nødvendig pga. foreign key til Plant)
INSERT INTO dbo.[User] (UserName, PasswordHash, Subscription, Plants_amount)
VALUES (
    'dummyuser',
    'abc123hashedpassword',  -- For at undgå at have et password i databsen
    'Free',
    10
);
GO

-- Så kan vi tilføje planter til brugeren
INSERT INTO dbo.Plant (Plant_Name, Plant_type, User_ID)
VALUES ('Test plant', 'Dummy', 1);

INSERT INTO dbo.Plant (Plant_Name, Plant_type, User_ID)
VALUES ('Demo Plant', 'Succulent', 1);
GO

-- Og et par logge
INSERT INTO dbo.PlantLog (Plant_ID, TemperatureLevel, WaterLevel, AirHumidityLevel)
VALUES (1, 21.5, 45.0, 55.0);
GO

-- Og en notifikation
INSERT INTO dbo.Notification (Plant_Type, Plant_ID, User_ID)
VALUES ('Dummy', 1, 1);
GO

/* Fjern for at droppe alle tabeller
-- DROP TABLE dbo.Notification;
-- DROP TABLE dbo.PlantLog;
-- DROP TABLE dbo.Plant;
-- DROP TABLE dbo.[User];
*/