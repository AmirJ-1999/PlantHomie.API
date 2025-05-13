/* =========================================================
   0)   Opret selve databasen  (spring over hvis den findes)
   ========================================================= */
IF DB_ID('PlantHomie') IS NULL
    CREATE DATABASE PlantHomie;
GO
USE PlantHomie;
GO

/* =========================================================
   1)   Brugere
   ========================================================= */
CREATE TABLE dbo.[User] (
    User_ID        INT            IDENTITY(1,1) PRIMARY KEY,
    Name           VARCHAR(50)    NOT NULL,
    Email          VARCHAR(50)    NOT NULL UNIQUE,
    Information    TEXT           NULL,
    Plants_amount  INT            NULL
);

------------------------------------------------------------
/* =========================================================
   2)   Planter
   ========================================================= */
CREATE TABLE dbo.Plant (
    Plant_ID     INT            IDENTITY(1,1) PRIMARY KEY,
    Plant_Name   VARCHAR(50)    UNIQUE,
    Plant_type   VARCHAR(50),
    ImageUrl     NVARCHAR(255)  NULL
);

------------------------------------------------------------
/* =========================================================
   3)   Notifikationer
   ========================================================= */
CREATE TABLE dbo.Notification (
    Notification_ID INT         IDENTITY(1,1) PRIMARY KEY,
    Dato_Tid        DATETIME     DEFAULT(GETUTCDATE()),
    Plant_Type      VARCHAR(50),
    Plant_ID        INT          NOT NULL,
    User_ID         INT          NOT NULL,
    CONSTRAINT FK_Notification_Plant FOREIGN KEY (Plant_ID)
        REFERENCES dbo.Plant (Plant_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Notification_User  FOREIGN KEY (User_ID)
        REFERENCES dbo.[User] (User_ID) ON DELETE CASCADE
);

------------------------------------------------------------
/* =========================================================
   4)   Plantelog (sensor-målinger)
   ========================================================= */
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


   --5)   (Valgfrit) Seed et par rækker til test

INSERT dbo.[User] (Name, Email, Information, Plants_amount)
VALUES ('Test-Bruger', 'test@demo.local', NULL, 1);

INSERT dbo.Plant (Plant_Name, Plant_type)
VALUES ('Demo Plant', 'Succulent');

INSERT dbo.PlantLog (Plant_ID, TemperatureLevel, WaterLevel, AirHumidityLevel)
VALUES (1, 21.5, 45.0, 55.0);
GO
