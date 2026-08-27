using System.Data;
using System.Data.SqlClient;
using UnityEngine;

/// <summary>
/// The bridge between Unity and AetherRealmDB. Every method here
/// wraps one stored procedure from Step 1, so nowhere else in the
/// game ever writes raw SQL — gameplay code just calls plain C#
/// methods like SavePlayerState() or AddItemToInventory().
///
/// Attach this to one persistent GameObject in your scene (e.g. an
/// empty object named "DatabaseManager").
/// </summary>
public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    [Tooltip("Matches the .\\SQLEXPRESS instance and AetherRealmDB you created in Step 1.")]
    [SerializeField]
    private string connectionString =
        "Server=.\\SQLEXPRESS;Database=AetherRealmDB;Trusted_Connection=True;TrustServerCertificate=True;";

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

    /// <summary>Creates a new player row and returns its generated PlayerId.</summary>
    public int CreatePlayer(string username)
    {
        using (var conn = GetConnection())
        using (var cmd = new SqlCommand("sp_CreatePlayer", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);

            var outputParam = new SqlParameter("@NewPlayerId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outputParam);

            conn.Open();
            cmd.ExecuteNonQuery();

            return (int)outputParam.Value;
        }
    }

    /// <summary>Saves level, stats, and position in one transactional call.</summary>
    public void SavePlayerState(int playerId, int level, int experience, int gold,
        int health, int maxHealth, Vector3 position, int districtId)
    {
        using (var conn = GetConnection())
        using (var cmd = new SqlCommand("sp_SavePlayerState", conn))
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
            Debug.Log($"Saved player {playerId} to the database.");
        }
    }

    /// <summary>Loads a player's saved state. Returns false if no row was found.</summary>
    public bool LoadPlayerState(int playerId, out PlayerSaveData data)
    {
        data = default;

        using (var conn = GetConnection())
        using (var cmd = new SqlCommand("sp_LoadPlayerState", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PlayerId", playerId);

            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                    return false;

                data = new PlayerSaveData
                {
                    PlayerId = reader.GetInt32(reader.GetOrdinal("PlayerId")),
                    Username = reader.GetString(reader.GetOrdinal("Username")),
                    Level = reader.GetInt32(reader.GetOrdinal("Level")),
                    Experience = reader.GetInt32(reader.GetOrdinal("Experience")),
                    Gold = reader.GetInt32(reader.GetOrdinal("Gold")),
                    Health = reader.GetInt32(reader.GetOrdinal("Health")),
                    MaxHealth = reader.GetInt32(reader.GetOrdinal("MaxHealth")),
                    Position = new Vector3(
                        (float)reader.GetDouble(reader.GetOrdinal("PosX")),
                        (float)reader.GetDouble(reader.GetOrdinal("PosY")),
                        (float)reader.GetDouble(reader.GetOrdinal("PosZ")))
                };

                return true;
            }
        }
    }

    /// <summary>Adds an item to a player's inventory, stacking if they already have it.</summary>
    public void AddItemToInventory(int playerId, int itemId, int quantity = 1)
    {
        using (var conn = GetConnection())
        using (var cmd = new SqlCommand("sp_AddItemToInventory", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PlayerId", playerId);
            cmd.Parameters.AddWithValue("@ItemId", itemId);
            cmd.Parameters.AddWithValue("@Quantity", quantity);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Removes/consumes an item from a player's inventory.</summary>
    public void RemoveItemFromInventory(int playerId, int itemId, int quantity = 1)
    {
        using (var conn = GetConnection())
        using (var cmd = new SqlCommand("sp_RemoveItemFromInventory", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PlayerId", playerId);
            cmd.Parameters.AddWithValue("@ItemId", itemId);
            cmd.Parameters.AddWithValue("@Quantity", quantity);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}

/// <summary>Plain data returned by LoadPlayerState — mirrors the Players table.</summary>
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
