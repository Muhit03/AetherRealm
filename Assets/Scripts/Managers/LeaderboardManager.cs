using System;
using System.Collections.Generic;
using UnityEngine;

// Handles loading leaderboard data from the database
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    private List<LeaderboardEntry> cachedEntries = new List<LeaderboardEntry>();
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

    public void Refresh()
    {
        if (isFetching || DatabaseManager.Instance == null)
        {
            return;
        }

        isFetching = true;

        try
        {
            cachedEntries = DatabaseManager.Instance.GetLeaderboard();
            Debug.Log("Fetched " + cachedEntries.Count + " leaderboard entries.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to fetch leaderboard: " + ex.Message);
            cachedEntries = new List<LeaderboardEntry>();
        }
        finally
        {
            isFetching = false;
        }
    }

    public List<LeaderboardEntry> GetCachedEntries()
    {
        return new List<LeaderboardEntry>(cachedEntries);
    }
}
