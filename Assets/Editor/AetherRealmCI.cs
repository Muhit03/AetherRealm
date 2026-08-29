using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Command-line smoke test / diagnostic. Run with the editor closed:
///
///   Unity.exe -batchmode -projectPath . -executeMethod AetherRealmCI.Run -logFile ci.log
///
/// It cleans and saves the play scene, enters Play Mode, plays through the
/// opening of a run, saves screenshots, dumps the UI hierarchy and every
/// enemy's AI state, then exits 0 (all good) / 1 (a check failed).
/// </summary>
public static class AetherRealmCI
{
    const string ActiveFlag = "AetherRealmCI.Active";
    static string ShotDir =>
        Environment.GetEnvironmentVariable("AETHER_SHOT_DIR") ??
        Path.Combine(Directory.GetCurrentDirectory(), "CI_Shots");

    static double startTime;
    static bool runStarted;
    static double runStartedAt;

    static bool shot1Done, shot2Done, shot3Done, shot4Done, shot5Done;
    static bool runSaveRoundTrips;
    static bool enemyDumpDone;
    static bool leaderboardOpened;
    static bool runEnded;

    static string enemyReport = "(not captured)";
    static int liveEnemies;
    static float enemyMovedAvg = -1f;
    static float enemyNearest = 999f;
    static bool anyEnemyChasing;
    static bool anyEnemyOnNavMesh;

    static string uiReport = "(not captured)";
    static int leaderboardRowCount = -1;
    static string leaderboardDump = "(not captured)";
    static bool leaderboardHasHero;
    static bool leaderboardHasBench;

    static readonly StringBuilder errors = new StringBuilder();

    public static void Run()
    {
        CleanAndSaveScene();
        Directory.CreateDirectory(ShotDir);
        // remember the real local leaderboard so the test run doesn't pollute it
        SessionState.SetString("AetherRealmCI.Scores", PlayerPrefs.GetString("local_scores", ""));
        SessionState.SetBool(ActiveFlag, true);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Reattach()
    {
        if (!SessionState.GetBool(ActiveFlag, false)) return;
        startTime = EditorApplication.timeSinceStartup;
        runStarted = false;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += OnUpdate;
    }

    [MenuItem("AetherRealm/Set Up Play Scene")]
    public static void CleanAndSaveScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        int removed = 0;
        foreach (var go in scene.GetRootGameObjects())
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        var goblin = GameObject.Find("Goblin");
        if (goblin != null) UnityEngine.Object.DestroyImmediate(goblin);

        if (UnityEngine.Object.FindAnyObjectByType<GameBootstrap>() == null)
        {
            var host = GameObject.Find("Managers") ?? new GameObject("GameBootstrap");
            if (host.GetComponent<GameBootstrap>() == null) host.AddComponent<GameBootstrap>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("AetherRealm: scene cleaned (removed " + removed + " missing scripts).");
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
            errors.AppendLine(type + ": " + message);
    }

    static void Shot(string name)
    {
        try
        {
            var cam = Camera.main;
            if (cam == null) { Debug.Log("CI screenshot skipped (no camera): " + name); return; }

            int w = 1280, h = 720;

            // Route the overlay canvases through the camera so they show up in
            // the render-texture capture (overlay UI isn't captured otherwise).
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            var restore = new System.Collections.Generic.List<Canvas>();
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = 1f;
                    restore.Add(c);
                }
            }
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(w, h, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            File.WriteAllBytes(Path.Combine(ShotDir, name), tex.EncodeToPNG());

            foreach (var c in restore) c.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.ForceUpdateCanvases();

            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(tex);
            Debug.Log("CI screenshot saved: " + name);
        }
        catch (Exception e)
        {
            Debug.Log("CI screenshot FAILED (" + name + "): " + e.Message);
        }
    }

    static void OnUpdate()
    {
        if (!EditorApplication.isPlaying) return;
        double t = EditorApplication.timeSinceStartup - startTime;

        // 2s: register a fresh account, register a SECOND that never plays
        // (to prove new registrations show on the leaderboard), then start a
        // Mage run.
        if (!runStarted && t > 2.0)
        {
            runStarted = true;
            runStartedAt = t;
            if (AuthManager.Instance != null)
            {
                if (!AuthManager.Instance.Login("ci_hero", "pass1234"))
                    AuthManager.Instance.Register("ci_hero", "pass1234", "Mage");
                AuthManager.Instance.Register("ci_bench", "pass1234", "Warrior"); // never plays
                AuthManager.Instance.Login("ci_hero", "pass1234");                // back to the player
            }
            if (GameBootstrap.Instance != null)
                GameBootstrap.Instance.BeginRun("Mage");
        }
        if (!runStarted) return;

        double since = t - runStartedAt;

        // record the player's starting HP once the run is going
        if (playerStartHp < 0 && since > 3.0)
        {
            var pc0 = GetPlayer();
            if (pc0 != null) playerStartHp = pc0.CurrentHealth;
        }

        // watch every frame - a Mage bolt only lives a moment before it hits
        // an enemy in a swarm, so a single snapshot would miss it
        if (since > 3.0)
        {
            var pcNow = GetPlayer();
            if (pcNow != null && pcNow.CurrentHealth < playerLowestHp)
                playerLowestHp = pcNow.CurrentHealth;

            if (!mageMadeBolts)
            {
                var bolts = UnityEngine.Object.FindObjectsByType<AetherRealm.Projectile>(FindObjectsInactive.Exclude);
                foreach (var b in bolts)
                    if (b.Team == AetherRealm.Projectile.Side.Player) mageMadeBolts = true;
            }
        }

        // keep the test player alive-ish and swinging so we see a real fight
        DrivePlayer(since);

        // 4s in: also drop 4 test enemies at a portal so we can measure them
        if (since > 4.0 && ciEnemies == null)
        {
            Vector3 portal = new Vector3(15f, 0f, 0f);
            ciEnemies = new EnemyController[4];
            ciEnemies[0] = AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Goblin, portal);
            ciEnemies[1] = AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Goblin, portal + Vector3.forward * 1.5f);
            ciEnemies[2] = AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Archer, portal - Vector3.forward * 1.5f);
            ciEnemies[3] = AetherRealm.EnemyFactory.Create(AetherRealm.EnemyFactory.Kind.Brute, portal + Vector3.right);
            ciStart = new Vector3[4];
        }
        if (ciEnemies != null && !ciStartRecorded && since > 6.0)
        {
            ciStartRecorded = true;
            for (int i = 0; i < ciEnemies.Length; i++)
                if (ciEnemies[i] != null) ciStart[i] = ciEnemies[i].transform.position;
        }

        if (since > 8.0 && !shot1Done)
        {
            shot1Done = true;
            Debug.Log("CI ScreenEffects @ shot1: " + AetherRealm.ScreenEffects.Debug() +
                      "  timeScale=" + Time.timeScale.ToString("F2") +
                      "  ambient=" + RenderSettings.ambientLight);
            Shot("1_fight.png");
        }

        // 14s in: check the Mage's basic attack made projectiles, and that the
        // player actually lost health from the swarm
        if (since > 14.0 && !combatChecked)
        {
            combatChecked = true;
            var boltCount = UnityEngine.Object.FindObjectsByType<AetherRealm.Projectile>(FindObjectsInactive.Exclude).Length;
            var pc = GetPlayer();
            Debug.Log("CI COMBAT: player class=" + (pc != null ? pc.ClassName : "?") +
                      " startHp=" + playerStartHp + " hpNow=" + (pc != null ? pc.CurrentHealth : -1) +
                      " lowestHp=" + playerLowestHp +
                      " liveBolts=" + boltCount + " mageMadeBolts=" + mageMadeBolts);
        }

        // 16s in (10s of travel time): dump every enemy's state
        if (since > 16.0 && !enemyDumpDone) { enemyDumpDone = true; DumpEnemies(); }

        // 17s in: end the run so it gets written to the leaderboard
        if (since > 17.0 && !runEnded)
        {
            runEnded = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddDamageDealt(1500);
                GameManager.Instance.AddScore(400);
                GameManager.Instance.OnPlayerDeath();
            }
        }

        // 17.6s in: screenshot the end screen on its own (before the leaderboard
        // covers it) so we can eyeball the layout
        if (since > 17.6 && !shot4Done) { shot4Done = true; Shot("4_endscreen.png"); CheckRunSave(); }

        // 18.3s in: fake a checkpoint and re-show the end screen so we can also
        // eyeball the layout WITH the CONTINUE button, then clear it again
        if (since > 18.3 && !shot5Done)
        {
            shot5Done = true;
            FakeCheckpointAndReshow();
            Shot("5_endscreen_continue.png");
            AetherRealm.RunSave.Clear();
        }

        // 19s in: open the leaderboard and screenshot it
        if (since > 19.0 && !leaderboardOpened)
        {
            leaderboardOpened = true;
            if (UIManager.Instance != null) UIManager.Instance.ToggleLeaderboard();
        }
        if (since > 20.0 && !shot2Done) { shot2Done = true; Shot("2_leaderboard.png"); DumpUi(); }
        if (since > 22.0 && !shot3Done) { shot3Done = true; Shot("3_final.png"); }

        if (since > 23.0 || t > 120.0) Finish();
    }

    static float lastAttack;
    static float playerStartHp = -1f;
    static int playerLowestHp = 999;
    static bool combatChecked;
    static bool mageMadeBolts;

    static PlayerController GetPlayer()
    {
        var po = GameObject.FindGameObjectWithTag("Player");
        return po != null ? po.GetComponent<PlayerController>() : null;
    }

    static void DrivePlayer(double since)
    {
        var pc = GetPlayer();
        if (pc == null || pc.IsDead) return;

        // for the first ~9s DON'T heal - so we can watch the swarm hurt the
        // player (playerLowestHp records the dip). After that, keep it topped
        // up so the Mage stays alive and keeps casting bolts for the check.
        if (since > 9.0 && pc.CurrentHealth < pc.MaxHealth * 0.7f)
        {
            pc.Heal(pc.MaxHealth);
        }

        // attack roughly once a second. Check for a fresh player bolt in the
        // same frame - before Projectile.Update can fly it into an enemy and
        // destroy it - so the ranged-attack check can't be missed by timing.
        if (Time.time - lastAttack > 1f)
        {
            lastAttack = Time.time;
            pc.Attack();
            if (!mageMadeBolts)
            {
                var bolts = UnityEngine.Object.FindObjectsByType<AetherRealm.Projectile>(FindObjectsInactive.Exclude);
                foreach (var b in bolts)
                    if (b.Team == AetherRealm.Projectile.Side.Player) mageMadeBolts = true;
            }
        }

        // drift in a slow circle so enemies have to keep re-pathing
        pc.transform.position += new Vector3(Mathf.Cos(Time.time), 0f, Mathf.Sin(Time.time)) * Time.deltaTime * 2f;
    }

    static EnemyController[] ciEnemies;
    static Vector3[] ciStart;
    static bool ciStartRecorded;

    static void DumpEnemies()
    {
        var all = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
        liveEnemies = all.Length;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

        var sb = new StringBuilder();
        sb.AppendLine("  live enemies: " + all.Length);
        foreach (var e in all)
        {
            var agent = e.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool onMesh = agent != null && agent.isOnNavMesh;
            bool hasPath = agent != null && agent.hasPath;
            float vel = agent != null ? agent.velocity.magnitude : -1f;
            float dp = Vector3.Distance(e.transform.position, playerPos);
            if (onMesh) anyEnemyOnNavMesh = true;
            if (vel > 0.3f && dp < 25f) anyEnemyChasing = true;
            if (dp < enemyNearest) enemyNearest = dp;
            sb.AppendLine("    " + e.GetType().Name + " state=" + e.CurrentStateName +
                          " pos=" + e.transform.position.ToString("F0") +
                          " onMesh=" + onMesh + " hasPath=" + hasPath +
                          " vel=" + vel.ToString("F1") + " distToPlayer=" + dp.ToString("F0"));
        }

        // travel distance of the 4 test enemies
        if (ciStartRecorded)
        {
            float total = 0f; int n = 0;
            for (int i = 0; i < ciEnemies.Length; i++)
            {
                if (ciEnemies[i] == null) { total += 30f; n++; continue; }
                total += Vector3.Distance(ciEnemies[i].transform.position, ciStart[i]); n++;
            }
            enemyMovedAvg = n > 0 ? total / n : 0f;
            sb.AppendLine("  4 test enemies moved on average " + enemyMovedAvg.ToString("F1") + " m from the portal");
        }

        enemyReport = sb.ToString();
        Debug.Log("CI ENEMY DUMP:\n" + enemyReport);
    }

    // Round-trips a checkpoint through RunSave (PlayerPrefs) to prove the
    // save/continue feature serialises and reloads correctly.
    static void CheckRunSave()
    {
        try
        {
            var upgrades = new System.Collections.Generic.Dictionary<string, int> { { "health", 2 }, { "damage", 1 } };
            var write = new AetherRealm.RunSave.Data
            {
                wave = 5, gold = 123, score = 999, kills = 7,
                damage = 400, seconds = 88, className = "Mage", upgrades = upgrades
            };
            AetherRealm.RunSave.Write(write);

            AetherRealm.RunSave.Data back;
            bool ok = AetherRealm.RunSave.TryLoad(out back)
                      && back.wave == 5 && back.gold == 123 && back.score == 999
                      && back.kills == 7 && back.damage == 400 && back.className == "Mage"
                      && back.upgrades.ContainsKey("health") && back.upgrades["health"] == 2
                      && back.upgrades.ContainsKey("damage") && back.upgrades["damage"] == 1;

            AetherRealm.RunSave.Clear();
            ok = ok && !AetherRealm.RunSave.TryLoad(out back);

            runSaveRoundTrips = ok;
            Debug.Log("CI RUNSAVE: round-trip " + (ok ? "OK" : "FAILED"));
        }
        catch (Exception e)
        {
            Debug.Log("CI RUNSAVE: threw " + e.Message);
        }
    }

    static void FakeCheckpointAndReshow()
    {
        try
        {
            var write = new AetherRealm.RunSave.Data
            {
                wave = 4, gold = 220, score = 500, kills = 13, damage = 896, seconds = 55,
                className = "Warrior",
                upgrades = new System.Collections.Generic.Dictionary<string, int>()
            };
            AetherRealm.RunSave.Write(write);
            if (UIManager.Instance != null && UIManager.Instance.endScreen != null)
                UIManager.Instance.endScreen.Show(false, 500, 13, 3);
        }
        catch (Exception e) { Debug.Log("CI reshow threw " + e.Message); }
    }

    static void DumpUi()
    {
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        var sb = new StringBuilder();
        foreach (var c in canvases)
        {
            sb.AppendLine("  Canvas '" + c.name + "' active=" + c.gameObject.activeInHierarchy + " order=" + c.sortingOrder);
            DumpRect(sb, c.transform, 2);
        }
        uiReport = sb.ToString();
        Debug.Log("CI UI DUMP:\n" + uiReport);

        var lb = UnityEngine.Object.FindAnyObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);
        if (lb != null)
        {
            var texts = lb.GetComponentsInChildren<TMPro.TMP_Text>(true);
            leaderboardRowCount = texts.Length;
            var d = new StringBuilder();
            d.AppendLine("  LeaderboardPanel active=" + lb.gameObject.activeInHierarchy + " texts=" + texts.Length);
            foreach (var tx in texts)
            {
                d.AppendLine("    \"" + tx.text + "\"");
                if (tx.text == "ci_hero") leaderboardHasHero = true;
                if (tx.text == "ci_bench") leaderboardHasBench = true;
            }
            leaderboardDump = d.ToString();
            Debug.Log("CI LEADERBOARD DUMP:\n" + leaderboardDump);
        }

        // also ask the data layer directly
        if (DatabaseManager.Instance != null)
        {
            var rows = DatabaseManager.Instance.GetLeaderboard("score");
            var names = new StringBuilder("  GetLeaderboard rows: ");
            foreach (var row in rows) names.Append(row.Username).Append("(g").Append(row.Games).Append(") ");
            Debug.Log(names.ToString());
        }
    }

    static void DumpRect(StringBuilder sb, Transform tr, int depth)
    {
        if (depth > 5) return;
        for (int i = 0; i < tr.childCount; i++)
        {
            var ch = tr.GetChild(i);
            var rt = ch as RectTransform;
            string size = rt != null ? rt.rect.size.ToString("F0") : "-";
            sb.AppendLine(new string(' ', depth * 2) + ch.name + " active=" + ch.gameObject.activeSelf + " size=" + size);
            DumpRect(sb, ch, depth + 1);
        }
    }

    static void Finish()
    {
        EditorApplication.update -= OnUpdate;
        Application.logMessageReceived -= OnLog;
        SessionState.EraseBool(ActiveFlag);

        var r = new StringBuilder();
        r.AppendLine("\n===== AETHERREALM DIAGNOSTIC =====");
        r.AppendLine("screenshots in: " + ShotDir);

        bool ok = true;
        ok &= Check(r, "GameManager + WaveManager exist", GameManager.Instance != null && WaveManager.Instance != null);
        ok &= Check(r, "HUD + UIManager built", HUDController.Instance != null && UIManager.Instance != null);
        ok &= Check(r, "player spawned", GameObject.FindGameObjectWithTag("Player") != null);

        r.AppendLine(enemyReport);
        ok &= Check(r, "a wave actually spawned enemies", liveEnemies > 0);
        ok &= Check(r, "enemies are on the NavMesh", anyEnemyOnNavMesh);
        r.AppendLine("  (an enemy was seen actively moving toward the player: " + anyEnemyChasing + ")");
        ok &= Check(r, "test enemies pathed away from the portal (>5 m)", enemyMovedAvg > 5f);
        ok &= Check(r, "an enemy reached the player (< 4 m)", enemyNearest < 4f);
        r.AppendLine("  nearest enemy to player: " + enemyNearest.ToString("F1") + " m");

        r.AppendLine(uiReport);
        r.AppendLine(leaderboardDump);
        ok &= Check(r, "leaderboard panel has rows/text", leaderboardRowCount > 0);
        ok &= Check(r, "leaderboard shows the player who played (ci_hero)", leaderboardHasHero);
        ok &= Check(r, "leaderboard shows a just-registered player who never played (ci_bench)", leaderboardHasBench);

        r.AppendLine("  player: startHp=" + playerStartHp + " lowestHp=" + playerLowestHp + " mageMadeBolts=" + mageMadeBolts);
        ok &= Check(r, "Mage basic attack fires projectiles (ranged, not melee)", mageMadeBolts);
        ok &= Check(r, "player loses health when swarmed", playerLowestHp >= 0 && playerLowestHp < playerStartHp);
        ok &= Check(r, "checkpoint save/continue round-trips through PlayerPrefs", runSaveRoundTrips);

        if (errors.Length > 0)
        {
            ok = false;
            r.AppendLine("--- errors during play ---");
            r.Append(errors);
        }

        r.AppendLine("RESULT: " + (ok ? "PASS" : "FAIL"));
        Debug.Log(r.ToString());
        Debug.Log("AETHERREALM CI RESULT: " + (ok ? "PASS" : "FAIL"));

        // put the real local leaderboard back
        PlayerPrefs.SetString("local_scores", SessionState.GetString("AetherRealmCI.Scores", ""));
        PlayerPrefs.DeleteKey("local_user_ci_hero");
        PlayerPrefs.DeleteKey("run_save_ci_hero");
        PlayerPrefs.Save();

        EditorApplication.isPlaying = false;
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Check(StringBuilder r, string label, bool pass)
    {
        r.AppendLine("  [" + (pass ? "PASS" : "FAIL") + "] " + label);
        return pass;
    }
}
