using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Data.SqlClient;
using UnityEngine;

/// <summary>
/// Connects the game to a Microsoft SQL Server database and calls its stored
/// procedures with parameterised commands.
///
/// If the database can't be reached (server down, or the SQL client isn't
/// supported on this machine) every method quietly falls back to
/// <see cref="LocalStore"/> so the game still runs. <see cref="OfflineMode"/>
/// says which one is in use.
/// </summary>
public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance;

    [SerializeField]
    private string connectionString =
        "Server=localhost,1433;Database=AetherRealmDB;User Id=SA;Password=AetherRealm@2024;TrustServerCertificate=True;Connect Timeout=3;";

    /// <summary>True once a database call has failed and we've switched to the local fallback.</summary>
    public static bool OfflineMode { get; private set; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string path);

    static DatabaseManager()
    {
        // The native SQL Server networking library (Microsoft.Data.SqlClient.SNI.dll)
        // lives in the Plugins folder. Tell Windows where to find it.
        try
        {
            string[] folders =
            {
                Path.Combine(Application.dataPath, "Plugins", "SqlClient"),
                Path.Combine(Application.dataPath, "Plugins"),
                Path.GetDirectoryName(Application.dataPath), // build: next to the exe
            };
            foreach (string folder in folders)
            {
                if (folder != null && Directory.Exists(folder) &&
                    File.Exists(Path.Combine(folder, "Microsoft.Data.SqlClient.SNI.arm64.dll")))
                {
                    SetDllDirectory(folder);
                    return;
                }
            }
            // fall back to the plugins folder even if we didn't spot the SNI file
            string plugins = Path.Combine(Application.dataPath, "Plugins", "SqlClient");
            if (Directory.Exists(plugins))
            {
                SetDllDirectory(plugins);
            }
        }
        catch (Exception error)
        {
            Debug.LogWarning("Could not set the SQL native library path: " + error.Message);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(connectionString);
    }

    // Called once from a catch block the first time SQL fails.
    private static void GoOffline(Exception error)
    {
        if (!OfflineMode)
        {
            OfflineMode = true;
            Debug.LogWarning("Database unavailable, using local save instead. (" + error.Message + ")");
        }
    }

    // ---- accounts ----
    public int RegisterPlayer(string username, string passwordHash, string classType)
    {
        if (OfflineMode)
        {
            return LocalStore.RegisterPlayer(username, passwordHash, classType);
        }
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_RegisterPlayer", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@ClassType", classType);

                SqlParameter newId = new SqlParameter("@NewPlayerId", SqlDbType.Int);
                newId.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(newId);

                conn.Open();
                cmd.ExecuteNonQuery();
                return Convert.ToInt32(newId.Value);
            }
        }
        catch (Exception error)
        {
            GoOffline(error);
            return LocalStore.RegisterPlayer(username, passwordHash, classType);
        }
    }

    public int LoginPlayer(string username, string passwordHash, out string classType)
    {
        classType = "Warrior";
        if (OfflineMode)
        {
            return LocalStore.LoginPlayer(username, passwordHash, out classType);
        }
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_LoginPlayer", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                SqlParameter outId = new SqlParameter("@PlayerId", SqlDbType.Int);
                outId.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outId);

                SqlParameter outClass = new SqlParameter("@ClassType", SqlDbType.NVarChar, 20);
                outClass.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outClass);

                conn.Open();
                cmd.ExecuteNonQuery();

                if (outClass.Value != DBNull.Value && outClass.Value != null)
                {
                    classType = outClass.Value.ToString();
                }
                return Convert.ToInt32(outId.Value);
            }
        }
        catch (Exception error)
        {
            GoOffline(error);
            return LocalStore.LoginPlayer(username, passwordHash, out classType);
        }
    }

    public int CreatePlayer(string username)
    {
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_CreatePlayer", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);

                SqlParameter newId = new SqlParameter("@NewPlayerId", SqlDbType.Int);
                newId.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(newId);

                conn.Open();
                cmd.ExecuteNonQuery();
                return Convert.ToInt32(newId.Value);
            }
        }
        catch (Exception error)
        {
            GoOffline(error);
            return LocalStore.RegisterPlayer(username, "", "Warrior");
        }
    }

    // ---- player state ----
    public void SavePlayerState(int playerId, int level, int experience, int gold, int health,
        int maxHealth, Vector3 position, int districtId)
    {
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_SavePlayerState", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlayerId", playerId);
                cmd.Parameters.AddWithValue("@Level", level);
                cmd.Parameters.AddWithValue("@Experience", experience);
                cmd.Parameters.AddWithValue("@Gold", gold);
                cmd.Parameters.AddWithValue("@Health", health);
                cmd.Parameters.AddWithValue("@MaxHealth", maxHealth);
                cmd.Parameters.AddWithValue("@PosX", position.x);
                cmd.Parameters.AddWithValue("@PosY", position.y);
                cmd.Parameters.AddWithValue("@PosZ", position.z);
                cmd.Parameters.AddWithValue("@DistrictId", districtId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception error)
        {
            GoOffline(error);
            LocalStore.SavePlayerState(playerId, gold, health, maxHealth);
        }
    }

    public bool LoadPlayerState(int playerId, out PlayerSaveData data)
    {
        data = new PlayerSaveData();
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_LoadPlayerState", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlayerId", playerId);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return false;
                    }

                    data.PlayerId = reader.GetInt32(reader.GetOrdinal("PlayerId"));
                    data.Username = reader.GetString(reader.GetOrdinal("Username"));
                    data.Level = reader.GetInt32(reader.GetOrdinal("Level"));
                    data.Experience = reader.GetInt32(reader.GetOrdinal("Experience"));
                    data.Gold = reader.GetInt32(reader.GetOrdinal("Gold"));
                    data.Health = reader.GetInt32(reader.GetOrdinal("Health"));
                    data.MaxHealth = reader.GetInt32(reader.GetOrdinal("MaxHealth"));

                    float x = (float)reader.GetDouble(reader.GetOrdinal("PosX"));
                    float y = (float)reader.GetDouble(reader.GetOrdinal("PosY"));
                    float z = (float)reader.GetDouble(reader.GetOrdinal("PosZ"));
                    data.Position = new Vector3(x, y, z);
                    return true;
                }
            }
        }
        catch (Exception error)
        {
            GoOffline(error);
            return false; // offline: start fresh
        }
    }

    // ---- inventory (kept for the course; not used by the survival loop) ----
    public void AddItemToInventory(int playerId, int itemId, int quantity = 1)
    {
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_AddItemToInventory", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlayerId", playerId);
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception error)
        {
            GoOffline(error);
        }
    }

    public void RemoveItemFromInventory(int playerId, int itemId, int quantity = 1)
    {
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_RemoveItemFromInventory", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlayerId", playerId);
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception error)
        {
            GoOffline(error);
        }
    }

    // ---- leaderboard ----
    public void SaveScore(int playerId, int score, int kills, int waves, int damage, int playTimeSecs)
    {
        if (OfflineMode)
        {
            LocalStore.SaveScore(AuthManager.CurrentUsername, AuthManager.CurrentClassType, score, kills, waves, damage, playTimeSecs);
            return;
        }
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_SaveScore", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlayerId", playerId);
                cmd.Parameters.AddWithValue("@Score", score);
                cmd.Parameters.AddWithValue("@Kills", kills);
                cmd.Parameters.AddWithValue("@Waves", waves);
                cmd.Parameters.AddWithValue("@Damage", damage);
                cmd.Parameters.AddWithValue("@PlayTimeSecs", playTimeSecs);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception error)
        {
            GoOffline(error);
            LocalStore.SaveScore(AuthManager.CurrentUsername, AuthManager.CurrentClassType, score, kills, waves, damage, playTimeSecs);
        }
    }

    // sortBy: "score" | "waves" | "kills" | "damage"
    public List<LeaderboardEntry> GetLeaderboard(string sortBy)
    {
        if (OfflineMode)
        {
            return LocalStore.GetLeaderboard(sortBy);
        }

        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        try
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_GetLeaderboard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SortBy", sortBy);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        LeaderboardEntry entry = new LeaderboardEntry();
                        entry.Rank = reader.GetInt32(reader.GetOrdinal("Rank"));
                        entry.Username = reader.GetString(reader.GetOrdinal("Username"));
                        entry.ClassType = reader.GetString(reader.GetOrdinal("ClassType"));
                        entry.Score = reader.GetInt32(reader.GetOrdinal("Score"));
                        entry.Waves = reader.GetInt32(reader.GetOrdinal("Waves"));
                        entry.Kills = reader.GetInt32(reader.GetOrdinal("Kills"));
                        entry.Damage = reader.GetInt32(reader.GetOrdinal("Damage"));
                        entry.Games = reader.GetInt32(reader.GetOrdinal("Games"));
                        entries.Add(entry);
                    }
                }
            }
            return entries;
        }
        catch (Exception error)
        {
            GoOffline(error);
            return LocalStore.GetLeaderboard(sortBy);
        }
    }
}

// Structs for player save data and leaderboard entries
[System.Serializable]
public struct PlayerSaveData
{
    public int PlayerId;
    public string Username;
    public int Level;
    public int Experience;
    public int Gold;
    public int Health;
    public int MaxHealth;
    public Vector3 Position;
}

[System.Serializable]
public struct LeaderboardEntry
{
    public int Rank;
    public string Username;
    public string ClassType;
    public int Score;
    public int Waves;
    public int Kills;
    public int Damage;
    public int Games;     // how many runs this player has finished
}
