using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AetherRealm;

/// <summary>
/// Runs the arena survival loop: announce a wave, spawn its enemies from the
/// portals, wait until they are all dead, give a shop break, then start the next
/// wave. Each wave has more enemies, more enemy types and tougher stats than the
/// last. Wave 8 is the boss.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public int totalWaves = 8;
    public float shopBreakSeconds = 18f;
    public int maxEnemiesAtOnce = 12;   // spawn the rest as these die - keeps it smooth

    ArenaLayout arena;
    List<EnemyController> livingEnemies = new List<EnemyController>();
    int currentWave;
    int wavesCleared;
    bool running;
    bool skipRequested;

    public int CurrentWave { get { return currentWave; } }
    public int WavesCleared { get { return wavesCleared; } }
    public int TotalWaves { get { return totalWaves; } }
    public int EnemiesAlive { get { return livingEnemies.Count; } }

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        EnemyController.Died += OnEnemyDied;
    }

    void OnDisable()
    {
        EnemyController.Died -= OnEnemyDied;
    }

    public void StartWaves(ArenaLayout arenaLayout)
    {
        arena = arenaLayout;
        if (!running)
        {
            StartCoroutine(RunAllWaves());
        }
    }

    public void SkipShopBreak()
    {
        skipRequested = true;
    }

    void OnEnemyDied(EnemyController enemy)
    {
        livingEnemies.Remove(enemy);
    }

    IEnumerator RunAllWaves()
    {
        running = true;
        yield return new WaitForSeconds(2f);

        // a resumed run starts on its saved wave instead of wave 1
        int startWave = GameManager.Instance != null ? GameManager.Instance.ResumeWave : 1;
        if (startWave < 1) startWave = 1;
        wavesCleared = startWave - 1;

        for (currentWave = startWave; currentWave <= totalWaves; currentWave++)
        {
            bool isBossWave = (currentWave == totalWaves);

            // checkpoint: from wave 2 on, save so a death here can be resumed
            if (currentWave >= 2 && GameManager.Instance != null)
            {
                GameManager.Instance.SaveCheckpoint(currentWave);
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.AnnounceWave(currentWave, totalWaves, isBossWave);
            }
            AudioManager.Play(AudioManager.Sound.WaveStart);
            yield return new WaitForSeconds(2.5f);

            yield return StartCoroutine(SpawnWave(currentWave, isBossWave));

            // wait until the arena is clear
            while (livingEnemies.Count > 0)
            {
                if (GameOver()) yield break;
                livingEnemies.RemoveAll(e => e == null);
                yield return null;
            }

            if (GameOver()) yield break;

            wavesCleared = currentWave;

            if (currentWave < totalWaves)
            {
                int bonus = 60 + currentWave * 30;
                if (GameManager.Instance != null) GameManager.Instance.AddScore(bonus);
                if (HUDController.Instance != null) HUDController.Instance.ShowWaveCleared(currentWave, bonus);
                yield return StartCoroutine(ShopBreak());
            }
        }

        running = false;
    }

    bool GameOver()
    {
        return GameManager.Instance != null && GameManager.Instance.IsGameOver;
    }

    IEnumerator ShopBreak()
    {
        skipRequested = false;
        if (ShopPanel.Instance != null) ShopPanel.Instance.Open();

        float timeLeft = shopBreakSeconds;
        while (timeLeft > 0f && !skipRequested)
        {
            timeLeft -= Time.deltaTime;
            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetIntermission(Mathf.CeilToInt(timeLeft));
            }
            yield return null;
        }

        if (ShopPanel.Instance != null) ShopPanel.Instance.Close();
        if (HUDController.Instance != null) HUDController.Instance.SetIntermission(0);
    }

    // ---- what to spawn each wave ----

    // Builds the list of enemy kinds for a normal wave. More enemies and more
    // variety the further you get.
    List<EnemyFactory.Kind> BuildPlan(int wave)
    {
        List<EnemyFactory.Kind> plan = new List<EnemyFactory.Kind>();

        int goblins = 2 + wave;                    // 3, 4, 5, 6 ...
        int archers = wave >= 3 ? wave - 2 : 0;    // 1, 2, 3 ... from wave 3
        int brutes  = wave >= 4 ? wave - 3 : 0;    // 1, 2, 3 ... from wave 4

        for (int i = 0; i < goblins; i++) plan.Add(EnemyFactory.Kind.Goblin);
        for (int i = 0; i < archers; i++) plan.Add(EnemyFactory.Kind.Archer);
        for (int i = 0; i < brutes;  i++) plan.Add(EnemyFactory.Kind.Brute);

        Shuffle(plan);
        return plan;
    }

    IEnumerator SpawnWave(int wave, bool boss)
    {
        float health = 1f + (wave - 1) * 0.16f;
        float damage = 1f + (wave - 1) * 0.10f;
        float speed  = 1f + (wave - 1) * 0.03f;
        float gap    = Mathf.Max(0.35f, 1.1f - wave * 0.08f);

        if (boss)
        {
            SpawnOne(EnemyFactory.Kind.Ogre, 1f, 1f, 1f);
            yield return new WaitForSeconds(3f);
        }

        List<EnemyFactory.Kind> plan = boss
            ? new List<EnemyFactory.Kind> {
                EnemyFactory.Kind.Goblin, EnemyFactory.Kind.Goblin, EnemyFactory.Kind.Archer,
                EnemyFactory.Kind.Goblin, EnemyFactory.Kind.Brute, EnemyFactory.Kind.Archer,
                EnemyFactory.Kind.Goblin, EnemyFactory.Kind.Goblin }
            : BuildPlan(wave);

        foreach (EnemyFactory.Kind kind in plan)
        {
            // hold back if the arena is already full
            while (livingEnemies.Count >= maxEnemiesAtOnce)
            {
                if (GameOver()) yield break;
                yield return null;
            }

            SpawnOne(kind, health, damage, speed);
            yield return new WaitForSeconds(Random.Range(gap * 0.6f, gap * 1.4f));
        }
    }

    void SpawnOne(EnemyFactory.Kind kind, float health, float damage, float speed)
    {
        Vector3 position = ChooseSpawnPoint();
        EnemyController enemy = EnemyFactory.Create(kind, position);
        enemy.Scale(health, damage, speed);
        livingEnemies.Add(enemy);

        Effects.Sparks(position + Vector3.up, Palette.Portal, 10);
        AudioManager.Play(AudioManager.Sound.Portal);
    }

    Vector3 ChooseSpawnPoint()
    {
        if (arena == null || arena.spawnPoints.Count == 0)
        {
            return new Vector3(Random.Range(-8f, 8f), 1f, Random.Range(-8f, 8f));
        }
        return arena.spawnPoints[Random.Range(0, arena.spawnPoints.Count)];
    }

    static void Shuffle(List<EnemyFactory.Kind> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            EnemyFactory.Kind temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
