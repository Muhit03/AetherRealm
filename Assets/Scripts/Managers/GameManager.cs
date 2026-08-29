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

    // When the player picks "Continue", the scene reloads to clear everything
    // out. A static field survives a scene load, so the fresh GameManager reads
    // this and picks the run back up from the saved checkpoint.
    public static bool ResumeRequested;

    int score;
    int kills;
    int damageDealt;
    float startTime;
    int resumeWave = 1;
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
    public int ResumeWave { get { return resumeWave; } }

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

        // Are we carrying on from a saved checkpoint?
        RunSave.Data saved = default(RunSave.Data);
        bool resuming = false;
        if (ResumeRequested && RunSave.TryLoad(out saved))
        {
            resuming = true;
        }
        ResumeRequested = false;

        if (resuming)
        {
            score = saved.score;
            kills = saved.kills;
            damageDealt = saved.damage;
            startTime = Time.time - saved.seconds;   // keep the run timer going
            resumeWave = saved.wave;
            upgradeLevels = saved.upgrades;
        }
        else
        {
            score = 0;
            kills = 0;
            damageDealt = 0;
            startTime = Time.time;
            resumeWave = 1;
            upgradeLevels.Clear();
        }

        state = RunState.Playing;
        Effects.ResetCounters();

        if (ScoreChanged != null) ScoreChanged(score);
        if (KillsChanged != null) KillsChanged(kills);

        if (player != null)
        {
            player.SetPlayerId(AuthManager.CurrentPlayerId);
            ApplyUpgrades();
            if (resuming)
            {
                player.RestoreProgress(saved.gold);   // gold back, full health
            }
        }

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPlayerInfo(player);
        }
    }

    // Called by WaveManager at the start of each wave (from wave 2 on). Writes a
    // checkpoint so the player can carry on from this wave later.
    public void SaveCheckpoint(int wave)
    {
        if (!AuthManager.IsLoggedIn || player == null)
        {
            return;
        }

        RunSave.Data d = new RunSave.Data();
        d.wave = wave;
        d.gold = player.Gold;
        d.score = score;
        d.kills = kills;
        d.damage = damageDealt;
        d.seconds = SecondsPlayed;
        d.className = player.ClassName;
        d.upgrades = upgradeLevels;
        RunSave.Write(d);
    }

    // "Continue" from the end screen: reload the scene and let StartRun pick the
    // run back up from the checkpoint.
    public void ContinueFromCheckpoint()
    {
        ResumeRequested = true;
        Time.timeScale = 1f;
        ScreenEffects.FadeOut();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
            RunSave.Clear();   // the run is finished - nothing to carry on from
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
        RunSave.Clear();          // "play again" / "restart" is a fresh run
        ResumeRequested = false;
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
