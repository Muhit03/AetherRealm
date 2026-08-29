using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AetherRealm
{
    /// <summary>
    /// A single "checkpoint" for the run in progress, kept on this PC with
    /// PlayerPrefs. It is written at the start of every wave, so if the player
    /// dies (or quits) they can carry on from that wave instead of starting the
    /// whole run again. One checkpoint is stored per account.
    /// </summary>
    public static class RunSave
    {
        // Everything we need to rebuild a run, packed into one line:
        // wave|gold|score|kills|damage|seconds|class|health,2;damage,1
        public struct Data
        {
            public int wave;
            public int gold;
            public int score;
            public int kills;
            public int damage;
            public int seconds;
            public string className;
            public Dictionary<string, int> upgrades;
        }

        static string Key()
        {
            string user = AuthManager.CurrentUsername;
            if (string.IsNullOrEmpty(user))
            {
                user = "guest";
            }
            return "run_save_" + user.ToLower();
        }

        // Is there a saved run for the account that is logged in?
        public static bool Has()
        {
            return AuthManager.IsLoggedIn && PlayerPrefs.HasKey(Key());
        }

        // The wave the saved run is on (0 if there is no save).
        public static int SavedWave()
        {
            Data d;
            return TryLoad(out d) ? d.wave : 0;
        }

        public static void Write(Data d)
        {
            StringBuilder ups = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in d.upgrades)
            {
                if (ups.Length > 0)
                {
                    ups.Append(";");
                }
                ups.Append(pair.Key).Append(",").Append(pair.Value);
            }

            string line = d.wave + "|" + d.gold + "|" + d.score + "|" + d.kills + "|" +
                          d.damage + "|" + d.seconds + "|" + d.className + "|" + ups;

            PlayerPrefs.SetString(Key(), line);
            PlayerPrefs.Save();
        }

        public static bool TryLoad(out Data d)
        {
            d = new Data();
            d.upgrades = new Dictionary<string, int>();

            string line = PlayerPrefs.GetString(Key(), "");
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            string[] parts = line.Split('|');
            if (parts.Length < 7)
            {
                return false;
            }

            int.TryParse(parts[0], out d.wave);
            int.TryParse(parts[1], out d.gold);
            int.TryParse(parts[2], out d.score);
            int.TryParse(parts[3], out d.kills);
            int.TryParse(parts[4], out d.damage);
            int.TryParse(parts[5], out d.seconds);
            d.className = parts[6];

            if (parts.Length >= 8 && parts[7].Length > 0)
            {
                foreach (string chunk in parts[7].Split(';'))
                {
                    string[] kv = chunk.Split(',');
                    if (kv.Length == 2)
                    {
                        int level;
                        int.TryParse(kv[1], out level);
                        d.upgrades[kv[0]] = level;
                    }
                }
            }

            if (d.wave < 1)
            {
                d.wave = 1;
            }
            return true;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key());
            PlayerPrefs.Save();
        }
    }
}
