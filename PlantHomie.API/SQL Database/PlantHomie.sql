CREATE TABLE [User] (
    User_ID INT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL,
    Email VARCHAR(50) NOT NULL,
    Information TEXT,
    Plants_amount INT
);

CREATE TABLE Plant (
    Plant_ID INT PRIMARY KEY,
    Plant_Name VARCHAR(50),
    Plant_type VARCHAR(50)
);

CREATE TABLE Notification (
    Notification_ID INT PRIMARY KEY,
    Dato_Tid DATETIME, 
    Plant_Type VARCHAR(50),
    Plant_ID INT,
    User_ID INT,
    FOREIGN KEY (User_ID) REFERENCES [User](User_ID),
    FOREIGN KEY (Plant_ID) REFERENCES Plant(Plant_ID)
);

CREATE TABLE PlantLog (
    PlantLog_ID INT IDENTITY(1,1) PRIMARY KEY, -- Sæt PlantLog_ID som en identitetskolonne direkte
    Plant_ID INT,
    Dato_Tid DATETIME,
    Temperaturelevel FLOAT,
    LightLevel FLOAT,
    WaterLevel FLOAT,
    AirHumidityLevel FLOAT,
    FOREIGN KEY (Plant_ID) REFERENCES Plant(Plant_ID)
);

     SELECT * FROM Plant WHERE Plant_ID = 1; -- Tester om der Plant_ID 1 eksisterer
     
   --  INSERT INTO Plant (Plant_ID, Plant_Name, Plant_type) VALUES (1, 'Example Plant', 'Example Type'); -- Indsætter en ny plante med Plant_ID 1, Plant_Name 'Example Plant' og Plant_type 'Example Type'
     