using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Class to manage player authentication and login state
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;

    private static int currentPlayerId = -1;
    private static string currentUsername = "";
    private static string currentClassType = "Warrior";

    public static int CurrentPlayerId
    {
        get { return currentPlayerId; }
    }

    public static string CurrentUsername
    {
        get { return currentUsername; }
    }

    public static string CurrentClassType
    {
        get { return currentClassType; }
    }

    public static bool IsLoggedIn
    {
        get { return currentPlayerId > 0; }
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

    public bool Register(string username, string password, string classType)
    {
        if (string.IsNullOrEmpty(username) || password.Length < 4)
        {
            Debug.LogWarning("Username empty or password too short.");
            return false;
        }

        string hash = HashPassword(password);
        int newId = DatabaseManager.Instance.RegisterPlayer(username, hash, classType);

        if (newId == -1)
        {
            Debug.LogWarning("Username " + username + " is already taken.");
            return false;
        }

        currentPlayerId = newId;
        currentUsername = username;
        currentClassType = classType;

        Debug.Log("Registered " + username + " successfully.");
        return true;
    }

    public bool Login(string username, string password)
    {
        string hash = HashPassword(password);
        string userClass = "";
        int id = DatabaseManager.Instance.LoginPlayer(username, hash, out userClass);

        if (id == -1)
        {
            Debug.LogWarning("Invalid login credentials.");
            return false;
        }

        currentPlayerId = id;
        currentUsername = username;
        currentClassType = userClass;

        Debug.Log("Logged in as " + username);
        return true;
    }

    public void Logout()
    {
        currentPlayerId = -1;
        currentUsername = "";
        currentClassType = "Warrior";
        Debug.Log("Logged out.");
    }

    // Helper method to hash password using SHA256
    private string HashPassword(string password)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
