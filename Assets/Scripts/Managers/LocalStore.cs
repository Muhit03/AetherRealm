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
    // One row per finished run. GetLeaderboard groups these by player and keeps
    // each player's best of every stat.
    [Serializable]
    class ScoreRow
    {
        public string username;
        public string classType;
        public int score;
        public int waves;
        public int kills;
        public int damage;
        public int playTime;
    }

    [Serializable]
    class ScoreList
    {
        public List<ScoreRow> rows = new List<ScoreRow>();
    }

    public static void SaveScore(string username, string classType, int score, int kills, int waves, int damage, int playTimeSecs)
    {
        ScoreList list = LoadScores();
        list.rows.Add(new ScoreRow
        {
            username = username,
            classType = classType,
            score = score,
            waves = waves,
            kills = kills,
            damage = damage,
            playTime = playTimeSecs
        });

        // keep the file from growing forever
        while (list.rows.Count > 200)
        {
            list.rows.RemoveAt(0);
        }

        PlayerPrefs.SetString("local_scores", JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    // sortBy: "score" | "waves" | "kills" | "damage"
    public static List<LeaderboardEntry> GetLeaderboard(string sortBy)
    {
        ScoreList list = LoadScores();

        // group every run by player and keep the best of each stat
        Dictionary<string, LeaderboardEntry> best = new Dictionary<string, LeaderboardEntry>();
        foreach (ScoreRow row in list.rows)
        {
            string key = row.username == null ? "" : row.username.ToLower();

            LeaderboardEntry entry;
            if (!best.TryGetValue(key, out entry))
            {
                entry = new LeaderboardEntry();
                entry.Username = row.username;
                entry.ClassType = row.classType;
            }

            entry.Score = Mathf.Max(entry.Score, row.score);
            entry.Waves = Mathf.Max(entry.Waves, row.waves);
            entry.Kills = Mathf.Max(entry.Kills, row.kills);
            entry.Damage = Mathf.Max(entry.Damage, row.damage);
            entry.Games += 1;
            entry.ClassType = row.classType;   // show their most recent class

            best[key] = entry;
        }

        List<LeaderboardEntry> entries = new List<LeaderboardEntry>(best.Values);

        entries.Sort((a, b) => SortValue(b, sortBy).CompareTo(SortValue(a, sortBy)));

        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry e = entries[i];
            e.Rank = i + 1;
            entries[i] = e;
        }

        if (entries.Count > 20)
        {
            entries.RemoveRange(20, entries.Count - 20);
        }
        return entries;
    }

    static int SortValue(LeaderboardEntry e, string sortBy)
    {
        if (sortBy == "waves") return e.Waves;
        if (sortBy == "kills") return e.Kills;
        if (sortBy == "damage") return e.Damage;
        return e.Score;
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
