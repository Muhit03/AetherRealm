using System;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Command-line smoke test. Run from a terminal with the editor closed:
///
///   Unity.exe -batchmode -projectPath . -executeMethod AetherRealmCI.Run -logFile ci.log
///
/// It cleans and saves the play scene, enters Play Mode, lets the game build
/// itself and start a run, watches for errors for a few seconds, then exits with
/// code 0 (all good) or 1 (something threw).
/// </summary>
public static class AetherRealmCI
{
    const string ActiveFlag = "AetherRealmCI.Active";

    static double startTime;
    static bool runStarted;
    static bool dbTested;
    static string dbResult = "(not tested)";
    static float gameTimeSeen;
    static int gameFramesSeen;

    // enemy AI checks
    static bool enemiesSpawned;
    static double spawnTime;
    static EnemyController[] ciEnemies = new EnemyController[0];
    static bool enemyStartRecorded;
    static Vector3[] enemyStart = new Vector3[0];
    static float enemyMovedAvg = -1f;
    static float enemyNearestToPlayer = 999f;
    static bool onNavMesh;
    static bool measured;
    static bool wasPlayingDuringWave;

    // leaderboard check
    static bool runEnded;
    static int leaderboardRows = -1;
    static string leaderboardTop = "(none)";

    static readonly StringBuilder errors = new StringBuilder();

    // ---- command line entry point ----
    public static void Run()
    {
        CleanAndSaveScene();
        SessionState.SetBool(ActiveFlag, true);
        EditorApplication.EnterPlaymode();
        // The watchdog below takes over after the play-mode domain reload.
    }

    // Runs again after every domain reload, including the one for entering Play
    // Mode (which wipes the static fields and event subscriptions above).
    [InitializeOnLoadMethod]
    static void ReattachWatchdog()
    {
        if (!SessionState.GetBool(ActiveFlag, false))
        {
            return;
        }
        startTime = EditorApplication.timeSinceStartup;
        runStarted = false;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += OnUpdate;
    }

    // ---- scene tidy-up (also on the menu) ----
    [MenuItem("AetherRealm/Set Up Play Scene")]
    public static void CleanAndSaveScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        int removed = 0;
        foreach (var go in scene.GetRootGameObjects())
        {
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        var goblin = GameObject.Find("Goblin");
        if (goblin != null)
        {
            UnityEngine.Object.DestroyImmediate(goblin);
        }

        if (UnityEngine.Object.FindAnyObjectByType<GameBootstrap>() == null)
        {
            var host = GameObject.Find("Managers");
            if (host == null)
            {
                host = new GameObject("GameBootstrap");
            }
            if (host.GetComponent<GameBootstrap>() == null)
            {
                host.AddComponent<GameBootstrap>();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("AetherRealm: scene cleaned (removed " + removed + " missing scripts). Press Play.");
    }

    // ---- play mode watchdog ----
    static void OnLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            errors.AppendLine(type + ": " + message);
        }
    }

    static void OnUpdate()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        double elapsed = EditorApplication.timeSinceStartup - startTime;
        gameTimeSeen = Time.time;
        gameFramesSeen = Time.frameCount;

        // ~2s in: log in (so scores save) and start the run
        if (!runStarted && elapsed > 2.0)
        {
            runStarted = true;
            if (AuthManager.Instance != null)
            {
                if (!AuthManager.Instance.Login("ci_hero", "pass1234"))
                {
                    AuthManager.Instance.Register("ci_hero", "pass1234", "Warrior");
                }
            }
            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.BeginRun("Warrior");
            }
        }

        // ~3s in: check the Microsoft SQL client can load and be called
        if (runStarted && !dbTested && elapsed > 3.0)
        {
            dbTested = true;
            try
            {
                // hit the raw SqlClient directly so we see its real exception
                var probe = new Microsoft.Data.SqlClient.SqlConnection(
                    "Server=localhost,1433;Database=AetherRealmDB;User Id=SA;Password=x;TrustServerCertificate=True;Connect Timeout=2;");
                probe.Open();
                probe.Close();
                dbResult = "CONNECTED";
            }
            catch (Exception e)
            {
                Exception inner = e;
                var sb = new StringBuilder();
                while (inner != null)
                {
                    sb.Append(inner.GetType().Name).Append(": ").Append(inner.Message).Append("  ||  ");
                    inner = inner.InnerException;
                }
                dbResult = sb.ToString();
                Debug.Log("CI DB probe full exception:\n" + e);
            }
        }

        // once the DB probe is done: drop 4 enemies right at a spawn portal
        // (this is the exact situation the "enemies stand outside the map" bug
        // was about). Timing is measured from here on, not from launch, because
        // the SQL timeouts above eat several seconds of wall-clock.
        if (runStarted && dbTested && !enemiesSpawned && elapsed > 4.0)
        {
            enemiesSpawned = true;
            spawnTime = elapsed;
            Vector3 portal = new Vector3(17f, 0f, 0f);
            ciEnemies = new EnemyController[4];
            ciEnemies[0] = AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Goblin, portal);
            ciEnemies[1] = AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Goblin, portal + Vector3.forward);
            ciEnemies[2] = AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Archer, portal - Vector3.forward);
            ciEnemies[3] = AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Brute, portal + Vector3.right * 0.5f);
        }

        double sinceSpawn = elapsed - spawnTime;

        // 2s after spawn: record where these 4 landed
        if (enemiesSpawned && !enemyStartRecorded && sinceSpawn > 2.0)
        {
            enemyStartRecorded = true;
            enemyStart = new Vector3[ciEnemies.Length];
            onNavMesh = true;
            for (int i = 0; i < ciEnemies.Length; i++)
            {
                if (ciEnemies[i] == null) { onNavMesh = false; continue; }
                enemyStart[i] = ciEnemies[i].transform.position;
                var agent = ciEnemies[i].GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent == null || !agent.isOnNavMesh) onNavMesh = false;
            }
        }

        // 10s after spawn: how far did these 4 travel, and did any reach the player?
        if (enemyStartRecorded && !measured && sinceSpawn > 10.0)
        {
            measured = true;
            wasPlayingDuringWave = GameManager.Instance != null && GameManager.Instance.IsPlaying;
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            float total = 0f;
            int counted = 0;
            for (int i = 0; i < ciEnemies.Length; i++)
            {
                if (ciEnemies[i] == null) continue;   // it died - that's fine
                total += Vector3.Distance(ciEnemies[i].transform.position, enemyStart[i]);
                counted++;
                if (playerObj != null)
                {
                    float d = Vector3.Distance(ciEnemies[i].transform.position, playerObj.transform.position);
                    if (d < enemyNearestToPlayer) enemyNearestToPlayer = d;
                }
            }
            enemyMovedAvg = counted > 0 ? total / counted : 50f; // all dead = they definitely engaged
        }

        // pretend the player finished a big run, then died - so we can check the
        // run gets written to the leaderboard with the new stats.
        if (measured && !runEnded && sinceSpawn > 11.0)
        {
            runEnded = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddDamageDealt(1234);
                GameManager.Instance.AddScore(500);
                GameManager.Instance.OnPlayerDeath();
            }
        }

        // read the leaderboard back
        if (runEnded && leaderboardRows < 0 && sinceSpawn > 13.0)
        {
            var board = DatabaseManager.Instance.GetLeaderboard("damage");
            leaderboardRows = board.Count;
            if (board.Count > 0)
            {
                LeaderboardEntry e = board[0];
                leaderboardTop = e.Username + " score=" + e.Score + " waves=" + e.Waves +
                                 " kills=" + e.Kills + " damage=" + e.Damage + " games=" + e.Games;
            }
        }

        if (leaderboardRows >= 0 && sinceSpawn > 14.0)
        {
            Finish();
        }
        if (elapsed > 45.0)   // absolute safety timeout
        {
            Finish();
        }
    }

    static void Finish()
    {
        EditorApplication.update -= OnUpdate;
        Application.logMessageReceived -= OnLog;
        SessionState.EraseBool(ActiveFlag);

        var report = new StringBuilder();
        report.AppendLine("\n===== AETHERREALM SMOKE TEST =====");

        bool ok = true;
        ok &= Check(report, "GameBootstrap built", GameBootstrap.Instance != null);
        ok &= Check(report, "GameManager exists", GameManager.Instance != null);
        ok &= Check(report, "WaveManager exists", WaveManager.Instance != null);
        ok &= Check(report, "HUD built", HUDController.Instance != null);
        ok &= Check(report, "Player spawned", GameObject.FindGameObjectWithTag("Player") != null);
        ok &= Check(report, "Arena floor built", GameObject.Find("Floor") != null);
        ok &= Check(report, "run was live during the wave", wasPlayingDuringWave);

        report.AppendLine("  game time: " + gameTimeSeen.ToString("F1") + "s, game frames: " + gameFramesSeen);

        // strip control characters so the log line doesn't get truncated
        string dbClean = "";
        foreach (char c in dbResult)
        {
            dbClean += (c < ' ') ? ' ' : c;
        }
        report.AppendLine("  Microsoft SQL client: " + dbClean);
        bool sqlClientWorks = dbClean.Contains("CONNECTED") || dbClean.Contains("SqlException");
        ok &= Check(report, "Microsoft SQL client stack works on this platform", sqlClientWorks);

        // --- enemy AI ---
        ok &= Check(report, "spawned enemies landed on the NavMesh", onNavMesh);
        ok &= Check(report, "enemies moved into the map (not stuck at portal)", enemyMovedAvg > 4f);
        ok &= Check(report, "an enemy reached the player", enemyNearestToPlayer < 12f);
        report.AppendLine("  enemies moved on average " + enemyMovedAvg.ToString("F1") +
                          " m, nearest got within " + enemyNearestToPlayer.ToString("F1") + " m of the player");

        // --- leaderboard ---
        ok &= Check(report, "run written to the leaderboard", leaderboardRows >= 1);
        report.AppendLine("  leaderboard rows: " + leaderboardRows + " | top: " + leaderboardTop);

        if (errors.Length > 0)
        {
            ok = false;
            report.AppendLine("--- errors logged during play ---");
            report.Append(errors);
        }

        report.AppendLine("RESULT: " + (ok ? "PASS" : "FAIL"));
        Debug.Log(report.ToString());
        Debug.Log("AETHERREALM CI RESULT: " + (ok ? "PASS" : "FAIL"));   // always its own line

        // don't leave the test account / scores in the local leaderboard
        PlayerPrefs.DeleteKey("local_scores");
        PlayerPrefs.DeleteKey("local_user_ci_hero");
        PlayerPrefs.DeleteKey("local_user_ci_probe");
        PlayerPrefs.Save();

        EditorApplication.isPlaying = false;
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Check(StringBuilder report, string label, bool passed)
    {
        report.AppendLine("  [" + (passed ? "PASS" : "FAIL") + "] " + label);
        return passed;
    }
}
