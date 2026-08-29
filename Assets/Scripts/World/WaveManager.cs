using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AetherRealm;

/// <summary>
/// Runs the arena survival loop: announce a wave, spawn enemies from the
/// portals a few at a time, wait until they are all dead, give a shop break,
/// then start the next wave. The last wave is the boss.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public int totalWaves = 8;
    public float shopBreakSeconds = 18f;

    ArenaLayout arena;
    List<EnemyController> livingEnemies = new List<EnemyController>();
    int currentWave;
    bool running;
    bool skipRequested;

    public int CurrentWave { get { return currentWave; } }
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

        for (currentWave = 1; currentWave <= totalWaves; currentWave++)
        {
            bool isBossWave = (currentWave == totalWaves);

            if (HUDController.Instance != null)
            {
                HUDController.Instance.AnnounceWave(currentWave, totalWaves, isBossWave);
            }
            AudioManager.Play(AudioManager.Sound.WaveStart);
            yield return new WaitForSeconds(2.5f);

            if (isBossWave)
            {
                yield return StartCoroutine(SpawnBossWave());
            }
            else
            {
                yield return StartCoroutine(SpawnNormalWave(currentWave));
            }

            // wait until the arena is clear
            while (livingEnemies.Count > 0)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                {
                    yield break;
                }
                livingEnemies.RemoveAll(e => e == null);
                yield return null;
            }

            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                yield break;
            }

            if (currentWave < totalWaves)
            {
                int bonus = 50 + currentWave * 25;
                if (GameManager.Instance != null) GameManager.Instance.AddScore(bonus);
                if (HUDController.Instance != null) HUDController.Instance.ShowWaveCleared(currentWave, bonus);
                yield return StartCoroutine(ShopBreak());
            }
        }

        running = false;
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

    IEnumerator SpawnNormalWave(int wave)
    {
        int enemyCount = 3 + wave * 2;
        float archerChance = 0.15f + wave * 0.05f;
        float healthMultiplier = 1f + (wave - 1) * 0.18f;
        float damageMultiplier = 1f + (wave - 1) * 0.12f;
        float speedMultiplier = 1f + (wave - 1) * 0.03f;

        for (int i = 0; i < enemyCount; i++)
        {
            EnemyFactory.Kind kind = (Random.value < archerChance)
                ? EnemyFactory.Kind.Archer
                : EnemyFactory.Kind.Goblin;

            SpawnEnemy(kind, healthMultiplier, damageMultiplier, speedMultiplier);
            yield return new WaitForSeconds(Random.Range(0.6f, 1.4f));
        }
    }

    IEnumerator SpawnBossWave()
    {
        SpawnEnemy(EnemyFactory.Kind.Ogre, 1f, 1f, 1f);
        yield return new WaitForSeconds(3f);

        for (int i = 0; i < 4; i++)
        {
            SpawnEnemy(EnemyFactory.Kind.Goblin, 1.4f, 1.2f, 1f);
            yield return new WaitForSeconds(2.5f);
        }
    }

    void SpawnEnemy(EnemyFactory.Kind kind, float health, float damage, float speed)
    {
        Vector3 position = ChooseSpawnPoint();
        EnemyController enemy = EnemyFactory.Create(kind, position);
        enemy.Scale(health, damage, speed);
        livingEnemies.Add(enemy);

        Effects.Sparks(position + Vector3.up, Palette.Portal, 16);
        AudioManager.Play(AudioManager.Sound.Portal);
    }

    Vector3 ChooseSpawnPoint()
    {
        if (arena == null || arena.spawnPoints.Count == 0)
        {
            return new Vector3(Random.Range(-8f, 8f), 1f, Random.Range(-8f, 8f));
        }
        int index = Random.Range(0, arena.spawnPoints.Count);
        return arena.spawnPoints[index].position;
    }
}
