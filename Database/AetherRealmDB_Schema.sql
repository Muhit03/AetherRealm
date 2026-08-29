-- =============================================================
--  AetherRealm Database Schema
--  Target:  Microsoft SQL Server (LocalDB / SQLEXPRESS)
--  Run via: sqlcmd -S .\SQLEXPRESS -i AetherRealmDB_Schema.sql
-- =============================================================

-- ── 0. Create / switch to the database ───────────────────────
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AetherRealmDB')
    CREATE DATABASE AetherRealmDB;
GO
USE AetherRealmDB;
GO

-- ── 1. TABLES ─────────────────────────────────────────────────

-- Players: one row per registered account
IF OBJECT_ID('dbo.Players', 'U') IS NULL
CREATE TABLE dbo.Players
(
    PlayerId      INT           IDENTITY(1,1) PRIMARY KEY,
    Username      NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(256) NOT NULL,          -- BCrypt or SHA-256 hex
    ClassType     NVARCHAR(20)  NOT NULL DEFAULT 'Warrior',  -- Warrior | Mage
    Level         INT           NOT NULL DEFAULT 1,
    Experience    INT           NOT NULL DEFAULT 0,
    Gold          INT           NOT NULL DEFAULT 50,
    Health        INT           NOT NULL DEFAULT 100,
    MaxHealth     INT           NOT NULL DEFAULT 100,
    CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- PlayerProgress: last saved world position
IF OBJECT_ID('dbo.PlayerProgress', 'U') IS NULL
CREATE TABLE dbo.PlayerProgress
(
    ProgressId  INT       IDENTITY(1,1) PRIMARY KEY,
    PlayerId    INT       NOT NULL REFERENCES dbo.Players(PlayerId) ON DELETE CASCADE,
    DistrictId  INT       NOT NULL DEFAULT 1,
    PosX        FLOAT     NOT NULL DEFAULT 0,
    PosY        FLOAT     NOT NULL DEFAULT 0,
    PosZ        FLOAT     NOT NULL DEFAULT 0,
    SavedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- Items: master list of loot / equipment
IF OBJECT_ID('dbo.Items', 'U') IS NULL
CREATE TABLE dbo.Items
(
    ItemId      INT           IDENTITY(1,1) PRIMARY KEY,
    ItemName    NVARCHAR(100) NOT NULL,
    ItemType    NVARCHAR(50)  NOT NULL,   -- Weapon | Armor | Consumable
    Value       INT           NOT NULL DEFAULT 0,
    Description NVARCHAR(255) NULL
);
GO

-- PlayerInventory: many-to-many between players and items
IF OBJECT_ID('dbo.PlayerInventory', 'U') IS NULL
CREATE TABLE dbo.PlayerInventory
(
    InventoryId INT IDENTITY(1,1) PRIMARY KEY,
    PlayerId    INT NOT NULL REFERENCES dbo.Players(PlayerId)   ON DELETE CASCADE,
    ItemId      INT NOT NULL REFERENCES dbo.Items(ItemId),
    Quantity    INT NOT NULL DEFAULT 1
);
GO

-- Leaderboard: one row per finished run
IF OBJECT_ID('dbo.Leaderboard', 'U') IS NULL
CREATE TABLE dbo.Leaderboard
(
    EntryId      INT       IDENTITY(1,1) PRIMARY KEY,
    PlayerId     INT       NOT NULL REFERENCES dbo.Players(PlayerId) ON DELETE CASCADE,
    Score        INT       NOT NULL DEFAULT 0,
    Waves        INT       NOT NULL DEFAULT 0,  -- waves fully cleared
    Kills        INT       NOT NULL DEFAULT 0,
    Damage       INT       NOT NULL DEFAULT 0,  -- total damage the player dealt
    PlayTimeSecs INT       NOT NULL DEFAULT 0,  -- seconds survived
    RecordedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- If the table already existed from an older version, add the new columns.
IF COL_LENGTH('dbo.Leaderboard', 'Waves') IS NULL
    ALTER TABLE dbo.Leaderboard ADD Waves INT NOT NULL DEFAULT 0;
GO
IF COL_LENGTH('dbo.Leaderboard', 'Damage') IS NULL
    ALTER TABLE dbo.Leaderboard ADD Damage INT NOT NULL DEFAULT 0;
GO

-- ── 2. STORED PROCEDURES ──────────────────────────────────────

-- sp_RegisterPlayer  —  create a new account (returns PlayerId or -1 on duplicate)
IF OBJECT_ID('dbo.sp_RegisterPlayer', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegisterPlayer;
GO
CREATE PROCEDURE dbo.sp_RegisterPlayer
    @Username     NVARCHAR(50),
    @PasswordHash NVARCHAR(256),
    @ClassType    NVARCHAR(20),
    @NewPlayerId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Players WHERE Username = @Username)
    BEGIN
        SET @NewPlayerId = -1;  -- duplicate username
        RETURN;
    END

    INSERT INTO dbo.Players (Username, PasswordHash, ClassType)
    VALUES (@Username, @PasswordHash, @ClassType);

    SET @NewPlayerId = SCOPE_IDENTITY();
END
GO

-- sp_LoginPlayer  —  validate credentials (returns PlayerId or -1)
IF OBJECT_ID('dbo.sp_LoginPlayer', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_LoginPlayer;
GO
CREATE PROCEDURE dbo.sp_LoginPlayer
    @Username     NVARCHAR(50),
    @PasswordHash NVARCHAR(256),
    @PlayerId     INT OUTPUT,
    @ClassType    NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @PlayerId  = PlayerId,
           @ClassType = ClassType
    FROM   dbo.Players
    WHERE  Username     = @Username
      AND  PasswordHash = @PasswordHash;

    IF @PlayerId IS NULL
        SET @PlayerId = -1;
END
GO

-- sp_CreatePlayer  —  quick-create without password (used by SaveLoadTester)
IF OBJECT_ID('dbo.sp_CreatePlayer', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CreatePlayer;
GO
CREATE PROCEDURE dbo.sp_CreatePlayer
    @Username    NVARCHAR(50),
    @NewPlayerId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Players (Username, PasswordHash)
    VALUES (@Username, 'NO_PASSWORD');
    SET @NewPlayerId = SCOPE_IDENTITY();
END
GO

-- sp_SavePlayerState  —  upsert stats + position in one transaction
IF OBJECT_ID('dbo.sp_SavePlayerState', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SavePlayerState;
GO
CREATE PROCEDURE dbo.sp_SavePlayerState
    @PlayerId   INT,
    @Level      INT,
    @Experience INT,
    @Gold       INT,
    @Health     INT,
    @MaxHealth  INT,
    @PosX       FLOAT,
    @PosY       FLOAT,
    @PosZ       FLOAT,
    @DistrictId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    -- Update player stats
    UPDATE dbo.Players
    SET    Level      = @Level,
           Experience = @Experience,
           Gold       = @Gold,
           Health     = @Health,
           MaxHealth  = @MaxHealth
    WHERE  PlayerId   = @PlayerId;

    -- Upsert position
    IF EXISTS (SELECT 1 FROM dbo.PlayerProgress WHERE PlayerId = @PlayerId)
        UPDATE dbo.PlayerProgress
        SET    DistrictId = @DistrictId,
               PosX       = @PosX,
               PosY       = @PosY,
               PosZ       = @PosZ,
               SavedAt    = SYSUTCDATETIME()
        WHERE  PlayerId   = @PlayerId;
    ELSE
        INSERT INTO dbo.PlayerProgress (PlayerId, DistrictId, PosX, PosY, PosZ)
        VALUES (@PlayerId, @DistrictId, @PosX, @PosY, @PosZ);

    COMMIT TRANSACTION;
END
GO

-- sp_LoadPlayerState  —  single SELECT joining Players + Progress
IF OBJECT_ID('dbo.sp_LoadPlayerState', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_LoadPlayerState;
GO
CREATE PROCEDURE dbo.sp_LoadPlayerState
    @PlayerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.PlayerId,
           p.Username,
           p.ClassType,
           p.Level,
           p.Experience,
           p.Gold,
           p.Health,
           p.MaxHealth,
           ISNULL(pp.PosX, 0)       AS PosX,
           ISNULL(pp.PosY, 0)       AS PosY,
           ISNULL(pp.PosZ, 0)       AS PosZ,
           ISNULL(pp.DistrictId, 1) AS DistrictId
    FROM   dbo.Players p
    LEFT JOIN dbo.PlayerProgress pp ON pp.PlayerId = p.PlayerId
    WHERE  p.PlayerId = @PlayerId;
END
GO

-- sp_AddItemToInventory  —  insert or stack
IF OBJECT_ID('dbo.sp_AddItemToInventory', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AddItemToInventory;
GO
CREATE PROCEDURE dbo.sp_AddItemToInventory
    @PlayerId INT,
    @ItemId   INT,
    @Quantity INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.PlayerInventory WHERE PlayerId = @PlayerId AND ItemId = @ItemId)
        UPDATE dbo.PlayerInventory
        SET    Quantity = Quantity + @Quantity
        WHERE  PlayerId = @PlayerId AND ItemId = @ItemId;
    ELSE
        INSERT INTO dbo.PlayerInventory (PlayerId, ItemId, Quantity)
        VALUES (@PlayerId, @ItemId, @Quantity);
END
GO

-- sp_RemoveItemFromInventory  —  decrement or delete
IF OBJECT_ID('dbo.sp_RemoveItemFromInventory', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RemoveItemFromInventory;
GO
CREATE PROCEDURE dbo.sp_RemoveItemFromInventory
    @PlayerId INT,
    @ItemId   INT,
    @Quantity INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.PlayerInventory
    SET    Quantity = Quantity - @Quantity
    WHERE  PlayerId = @PlayerId AND ItemId = @ItemId;

    DELETE FROM dbo.PlayerInventory
    WHERE  PlayerId = @PlayerId AND ItemId = @ItemId AND Quantity <= 0;
END
GO

-- sp_SaveScore  —  insert one leaderboard row at the end of a run
IF OBJECT_ID('dbo.sp_SaveScore', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SaveScore;
GO
CREATE PROCEDURE dbo.sp_SaveScore
    @PlayerId     INT,
    @Score        INT,
    @Kills        INT,
    @Waves        INT,
    @Damage       INT,
    @PlayTimeSecs INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Leaderboard (PlayerId, Score, Waves, Kills, Damage, PlayTimeSecs)
    VALUES (@PlayerId, @Score, @Waves, @Kills, @Damage, @PlayTimeSecs);
END
GO

-- sp_GetLeaderboard  —  one row per player showing their best of each stat.
-- @SortBy picks which column the table is ordered by.
IF OBJECT_ID('dbo.sp_GetLeaderboard', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetLeaderboard;
GO
CREATE PROCEDURE dbo.sp_GetLeaderboard
    @SortBy NVARCHAR(20) = 'score'   -- score | waves | kills | damage
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PlayerBest AS
    (
        SELECT p.PlayerId,
               p.Username,
               p.ClassType,
               MAX(lb.Score)  AS Score,
               MAX(lb.Waves)  AS Waves,
               MAX(lb.Kills)  AS Kills,
               MAX(lb.Damage) AS Damage,
               COUNT(*)       AS Games
        FROM   dbo.Players     p
        JOIN   dbo.Leaderboard lb ON lb.PlayerId = p.PlayerId
        GROUP BY p.PlayerId, p.Username, p.ClassType
    )
    SELECT TOP 20
           ROW_NUMBER() OVER (ORDER BY
               CASE @SortBy
                   WHEN 'waves'  THEN Waves
                   WHEN 'kills'  THEN Kills
                   WHEN 'damage' THEN Damage
                   ELSE Score
               END DESC) AS Rank,
           Username,
           ClassType,
           Score,
           Waves,
           Kills,
           Damage,
           Games
    FROM   PlayerBest
    ORDER BY Rank;
END
GO

-- ── 3. SEED DATA ──────────────────────────────────────────────

-- Starter items (safe to re-run thanks to IF NOT EXISTS)
IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemName = 'Iron Sword')
    INSERT INTO dbo.Items (ItemName, ItemType, Value, Description)
    VALUES ('Iron Sword',   'Weapon',     25, 'A reliable starter blade.'),
           ('Leather Armor','Armor',      20, 'Light protection for new adventurers.'),
           ('Health Potion','Consumable', 10, 'Restores 30 HP when consumed.'),
           ('Mage Staff',   'Weapon',     30, 'Channels arcane energy into ranged bolts.'),
           ('Shield',       'Armor',      35, 'Sturdy shield that blocks 10 damage per hit.');
GO

PRINT 'AetherRealmDB schema created successfully.';
GO
