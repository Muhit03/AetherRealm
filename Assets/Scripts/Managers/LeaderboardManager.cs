using System;
using System.Collections.Generic;
using UnityEngine;

// Loads the leaderboard from the database (or the local fallback) and caches it
// so the panel can ask for it without hitting the database every frame.
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    private List<LeaderboardEntry> cachedEntries = new List<LeaderboardEntry>();
    private string cachedSort = "";
    private bool isFetching = false;

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

    // sortBy: "score" | "waves" | "kills" | "damage"
    public void Refresh(string sortBy)
    {
        if (isFetching || DatabaseManager.Instance == null)
        {
            return;
        }

        isFetching = true;
        try
        {
            cachedEntries = DatabaseManager.Instance.GetLeaderboard(sortBy);
            cachedSort = sortBy;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not load the leaderboard: " + ex.Message);
            cachedEntries = new List<LeaderboardEntry>();
        }
        finally
        {
            isFetching = false;
        }
    }

    // Used by GameManager after a run - just refresh the default view.
    public void Refresh()
    {
        Refresh("score");
    }

    public List<LeaderboardEntry> GetCachedEntries()
    {
        return new List<LeaderboardEntry>(cachedEntries);
    }
}
