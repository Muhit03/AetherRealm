using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AetherRealm;

/// <summary>
/// Keeps track of one run: the score, the kills, the timer, the upgrades the
/// player has bought, and whether the run has been won or lost. A new one is
/// created every time the scene loads, so "restart" is just a scene reload.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum RunState { Menu, Playing, Lost, Won }

    public static GameManager Instance { get; private set; }

    // Other scripts (the HUD) listen to these to update themselves.
    public event Action<int> ScoreChanged;
    public event Action<int> KillsChanged;

    int score;
    int kills;
    int damageDealt;
    float startTime;
    RunState state = RunState.Menu;

    PlayerController player;
    Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

    public int Score { get { return score; } }
    public int Kills { get { return kills; } }
    public int DamageDealt { get { return damageDealt; } }
    public int WavesCleared { get { return WaveManager.Instance != null ? WaveManager.Instance.WavesCleared : 0; } }
    public int SecondsPlayed { get { return Mathf.FloorToInt(Time.time - startTime); } }
    public bool IsPlaying { get { return state == RunState.Playing; } }
    public bool IsGameOver { get { return state == RunState.Lost || state == RunState.Won; } }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void GoToMenu()
    {
        state = RunState.Menu;
    }

    // Called after the player has logged in and chosen a class.
    public void StartRun(PlayerController hero)
    {
        player = hero;
        score = 0;
        kills = 0;
        damageDealt = 0;
        startTime = Time.time;
        state = RunState.Playing;
        upgradeLevels.Clear();
        Effects.ResetCounters();

        if (ScoreChanged != null) ScoreChanged(score);
        if (KillsChanged != null) KillsChanged(kills);

        if (player != null)
        {
            player.SetPlayerId(AuthManager.CurrentPlayerId);
            ApplyUpgrades();
        }

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPlayerInfo(player);
        }
    }

    public void RegisterKill(int scoreForKill)
    {
        if (state != RunState.Playing)
        {
            return;
        }
        kills++;
        score += scoreForKill;
        if (KillsChanged != null) KillsChanged(kills);
        if (ScoreChanged != null) ScoreChanged(score);
    }

    public void AddScore(int amount)
    {
        if (state != RunState.Playing)
        {
            return;
        }
        score += amount;
        if (ScoreChanged != null) ScoreChanged(score);
    }

    // Called whenever the player deals damage. Feeds the leaderboard stat.
    public void AddDamageDealt(int amount)
    {
        if (state == RunState.Playing && amount > 0)
        {
            damageDealt += amount;
        }
    }

    // ---- shop upgrades ----
    public int GetUpgradeLevel(string id)
    {
        return upgradeLevels.ContainsKey(id) ? upgradeLevels[id] : 0;
    }

    public void BuyUpgrade(string id)
    {
        upgradeLevels[id] = GetUpgradeLevel(id) + 1;
        ApplyUpgrades();
    }

    void ApplyUpgrades()
    {
        if (player == null)
        {
            return;
        }
        player.bonusHealth = GetUpgradeLevel("health") * 25;
        player.bonusDamage = GetUpgradeLevel("damage") * 6;
        player.bonusSpeed = GetUpgradeLevel("speed") * 0.08f;
        player.cooldownScale = Mathf.Pow(0.85f, GetUpgradeLevel("cooldown"));
        player.lifesteal = GetUpgradeLevel("lifesteal") * 0.06f;

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPlayerInfo(player);
        }
    }

    // ---- end of run ----
    public void OnPlayerDeath()
    {
        if (IsGameOver)
        {
            return;
        }
        state = RunState.Lost;
        EndRun(false);
    }

    public void OnBossDefeated()
    {
        if (IsGameOver)
        {
            return;
        }
        state = RunState.Won;
        EndRun(true);
    }

    void EndRun(bool won)
    {
        if (won)
        {
            score += 1000;
        }

        SaveRunToLeaderboard();
        int waves = WavesCleared;
        if (UIManager.Instance != null) UIManager.Instance.ShowEndScreen(won, score, kills, waves);
    }

    // Writes the current run to the leaderboard (SQL Server, or the local
    // fallback). Called on death, victory, and when quitting mid-run.
    void SaveRunToLeaderboard()
    {
        if (score <= 0 && kills <= 0)
        {
            return; // nothing worth recording
        }
        if (AuthManager.IsLoggedIn && DatabaseManager.Instance != null)
        {
            try
            {
                DatabaseManager.Instance.SaveScore(
                    AuthManager.CurrentPlayerId, score, kills, WavesCleared, damageDealt, SecondsPlayed);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not save score: " + error.Message);
            }
        }
        if (LeaderboardManager.Instance != null) LeaderboardManager.Instance.Refresh();
    }

    public void Restart()
    {
        if (IsPlaying) { state = RunState.Lost; SaveRunToLeaderboard(); }
        Time.timeScale = 1f;
        ScreenEffects.FadeOut();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        if (IsPlaying) { state = RunState.Lost; SaveRunToLeaderboard(); }
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
