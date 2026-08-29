using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tiny offline stand-in for the SQL Server database, used automatically when
/// the database can't be reached (for example on a machine where the SQL client
/// isn't supported). It stores accounts and scores with Unity's PlayerPrefs so
/// the game stays fully playable. <see cref="DatabaseManager"/> falls back to
/// this on its own.
/// </summary>
public static class LocalStore
{
    // Each account is one PlayerPrefs string:
    //   key   "local_user_<name>"   value "<id>|<hash>|<class>|<gold>|<hp>|<maxhp>"
    // Plus a reverse lookup:
    //   key   "local_id_<id>"       value "<name>"

    static int NextId()
    {
        int id = PlayerPrefs.GetInt("local_next_id", 1);
        PlayerPrefs.SetInt("local_next_id", id + 1);
        return id;
    }

    public static int RegisterPlayer(string username, string passwordHash, string classType)
    {
        string key = "local_user_" + username.ToLower();
        if (PlayerPrefs.HasKey(key))
        {
            return -1; // username already taken
        }

        int id = NextId();
        PlayerPrefs.SetString(key, id + "|" + passwordHash + "|" + classType + "|0|100|100");
        PlayerPrefs.SetString("local_id_" + id, username.ToLower());
        PlayerPrefs.Save();
        return id;
    }

    public static int LoginPlayer(string username, string passwordHash, out string classType)
    {
        classType = "Warrior";

        string key = "local_user_" + username.ToLower();
        if (!PlayerPrefs.HasKey(key))
        {
            return -1;
        }

        string[] parts = PlayerPrefs.GetString(key).Split('|');
        if (parts.Length < 3 || parts[1] != passwordHash)
        {
            return -1; // wrong password
        }

        classType = parts[2];
        return int.Parse(parts[0]);
    }

    public static void SavePlayerState(int playerId, int gold, int health, int maxHealth)
    {
        string username = PlayerPrefs.GetString("local_id_" + playerId, "");
        if (username == "")
        {
            return;
        }

        string key = "local_user_" + username;
        string[] parts = PlayerPrefs.GetString(key).Split('|');
        if (parts.Length >= 3)
        {
            PlayerPrefs.SetString(key,
                parts[0] + "|" + parts[1] + "|" + parts[2] + "|" + gold + "|" + health + "|" + maxHealth);
            PlayerPrefs.Save();
        }
    }

    // ---- scores / leaderboard ----
    [Serializable]
    class ScoreRow
    {
        public string username;
        public string classType;
        public int score;
        public int kills;
        public int playTime;
    }

    [Serializable]
    class ScoreList
    {
        public List<ScoreRow> rows = new List<ScoreRow>();
    }

    public static void SaveScore(string username, string classType, int score, int kills, int playTimeSecs)
    {
        ScoreList list = LoadScores();
        list.rows.Add(new ScoreRow
        {
            username = username,
            classType = classType,
            score = score,
            kills = kills,
            playTime = playTimeSecs
        });

        list.rows.Sort((a, b) => b.score.CompareTo(a.score));
        while (list.rows.Count > 10)
        {
            list.rows.RemoveAt(list.rows.Count - 1);
        }

        PlayerPrefs.SetString("local_scores", JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    public static List<LeaderboardEntry> GetLeaderboard()
    {
        var entries = new List<LeaderboardEntry>();
        ScoreList list = LoadScores();

        for (int i = 0; i < list.rows.Count; i++)
        {
            ScoreRow row = list.rows[i];
            LeaderboardEntry entry = new LeaderboardEntry();
            entry.Rank = i + 1;
            entry.Username = row.username;
            entry.ClassType = row.classType;
            entry.Score = row.score;
            entry.Kills = row.kills;
            entry.PlayTimeSecs = row.playTime;
            entries.Add(entry);
        }
        return entries;
    }

    static ScoreList LoadScores()
    {
        string json = PlayerPrefs.GetString("local_scores", "");
        if (string.IsNullOrEmpty(json))
        {
            return new ScoreList();
        }

        try
        {
            ScoreList list = JsonUtility.FromJson<ScoreList>(json);
            return list != null ? list : new ScoreList();
        }
        catch
        {
            return new ScoreList();
        }
    }
}
