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
    static bool goblinSpawned;
    static bool goblinMeasured;
    static bool goblinOnNavMesh;
    static Vector3 goblinStartPos;
    static float goblinMoved = -1f;
    static string goblinDiag = "(not captured)";
    static float gameTimeSeen;
    static int gameFramesSeen;
    static string dbResult = "(not tested)";
    static bool dbTested;
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

        // ~2s in: log in and start the run
        if (!runStarted && elapsed > 2.0)
        {
            runStarted = true;
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

        // ~4s in: drop a goblin far from the player
        if (runStarted && !goblinSpawned && elapsed > 4.0)
        {
            goblinSpawned = true;
            AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Goblin, new Vector3(15f, 1f, 0f));
        }

        // ~6s in: record where it starts
        if (goblinSpawned && goblinStartPos == Vector3.zero && elapsed > 6.0)
        {
            var g = UnityEngine.Object.FindAnyObjectByType<MeleeGoblin>();
            if (g != null)
            {
                var agent = g.GetComponent<UnityEngine.AI.NavMeshAgent>();
                goblinOnNavMesh = agent != null && agent.isOnNavMesh;
                goblinStartPos = g.transform.position;
            }
        }

        // ~16s in: measure how far it travelled and dump agent diagnostics
        if (goblinStartPos != Vector3.zero && !goblinMeasured && elapsed > 16.0)
        {
            goblinMeasured = true;
            var g = UnityEngine.Object.FindAnyObjectByType<MeleeGoblin>();
            if (g != null)
            {
                goblinMoved = Vector3.Distance(g.transform.position, goblinStartPos);
                var agent = g.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    goblinDiag = "speed=" + agent.speed.ToString("F1") +
                                 " isStopped=" + agent.isStopped +
                                 " hasPath=" + agent.hasPath +
                                 " pathStatus=" + agent.pathStatus +
                                 " remaining=" + agent.remainingDistance.ToString("F1") +
                                 " vel=" + agent.velocity.magnitude.ToString("F2");
                }
            }
            else
            {
                goblinDiag = "(goblin no longer in scene - it may have been killed)";
            }
        }

        if (elapsed > 22.0)
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
        ok &= Check(report, "Run is playing", GameManager.Instance != null && GameManager.Instance.IsPlaying);

        var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include);
        report.AppendLine("  enemies in arena: " + enemies.Length);
        report.AppendLine("  score: " + (GameManager.Instance != null ? GameManager.Instance.Score : -1));
        report.AppendLine("  game time: " + gameTimeSeen.ToString("F1") + "s, game frames: " + gameFramesSeen);

        // strip control characters so the log line doesn't get truncated
        string dbClean = "";
        foreach (char c in dbResult)
        {
            dbClean += (c < ' ') ? ' ' : c;
        }
        report.AppendLine("  Microsoft SQL client: " + dbClean);
        // "CONNECTED" = server reachable; "SqlException" = client stack works, server just isn't up.
        // Anything else (TypeLoad / FileNotFound / PlatformNotSupported) means the client is broken.
        bool sqlClientWorks = dbClean.Contains("CONNECTED") || dbClean.Contains("SqlException");
        ok &= Check(report, "Microsoft SQL client stack works on this platform", sqlClientWorks);

        ok &= Check(report, "test goblin reached the NavMesh", goblinOnNavMesh);
        ok &= Check(report, "test goblin walked towards the player", goblinMoved > 1f);
        report.AppendLine("  goblin moved: " + goblinMoved.ToString("F1") + " m");
        report.AppendLine("  goblin agent: " + goblinDiag);

        if (errors.Length > 0)
        {
            ok = false;
            report.AppendLine("--- errors logged during play ---");
            report.Append(errors);
        }

        report.AppendLine("RESULT: " + (ok ? "PASS" : "FAIL"));
        Debug.Log(report.ToString());
        Debug.Log("AETHERREALM CI RESULT: " + (ok ? "PASS" : "FAIL"));   // always its own line

        EditorApplication.isPlaying = false;
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Check(StringBuilder report, string label, bool passed)
    {
        report.AppendLine("  [" + (passed ? "PASS" : "FAIL") + "] " + label);
        return passed;
    }
}
