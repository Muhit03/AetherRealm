using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AetherRealm;

/// <summary>
/// The in-game heads-up display: health/mana/stamina bars, gold, score, wave
/// info, the ability cooldown and the boss health bar. It builds all of its own
/// UI objects in code and other scripts update it through the Set* methods.
/// </summary>
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    Image healthBar;
    Image manaBar;
    Image staminaBar;
    Image abilityFill;
    TMP_Text healthText;
    TMP_Text goldText;
    TMP_Text scoreText;
    TMP_Text killsText;
    TMP_Text waveText;
    TMP_Text nameText;
    TMP_Text centerText;
    TMP_Text intermissionText;

    GameObject bossBarObject;
    Image bossBar;
    TMP_Text bossNameText;
    int bossMaxHealth = 1;

    string centerMessage = "";
    Color centerColor = Color.white;
    float centerTimer;

    PlayerController cachedPlayer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Build();
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ScoreChanged += SetScore;
            GameManager.Instance.KillsChanged += SetKills;
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ScoreChanged -= SetScore;
            GameManager.Instance.KillsChanged -= SetKills;
        }
    }

    void Build()
    {
        RectTransform root = (RectTransform)transform;
        UIFactory.Stretch(root);

        // --- bottom left: name + bars ---
        nameText = UIFactory.Label(root, "Hero", 28, TextAlignmentOptions.Left);
        Place(nameText.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 190f), new Vector2(500f, 36f));

        healthBar = UIFactory.Bar(root, Palette.Health, new Vector2(500f, 32f));
        Place((RectTransform)healthBar.transform.parent, new Vector2(0f, 0f), new Vector2(40f, 150f), new Vector2(500f, 32f));
        healthText = UIFactory.Label(healthBar.transform.parent, "100 / 100", 20, TextAlignmentOptions.Center);
        UIFactory.Stretch(healthText.rectTransform);

        manaBar = UIFactory.Bar(root, Palette.Mana, new Vector2(500f, 18f));
        Place((RectTransform)manaBar.transform.parent, new Vector2(0f, 0f), new Vector2(40f, 122f), new Vector2(500f, 18f));

        staminaBar = UIFactory.Bar(root, Palette.Stamina, new Vector2(500f, 12f));
        Place((RectTransform)staminaBar.transform.parent, new Vector2(0f, 0f), new Vector2(40f, 100f), new Vector2(500f, 12f));

        // --- bottom right: ability icon ---
        Image abilityBack = UIFactory.Box(root, "Ability", new Color(0f, 0f, 0f, 0.6f));
        Place(abilityBack.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(90f, 90f));
        abilityFill = UIFactory.Box(abilityBack.transform, "Fill", UIFactory.Accent);
        UIFactory.Stretch(abilityFill.rectTransform, 4f);
        abilityFill.type = Image.Type.Filled;
        abilityFill.fillMethod = Image.FillMethod.Radial360;
        TMP_Text abilityKey = UIFactory.Label(abilityBack.transform, "Q", 34, TextAlignmentOptions.Center);
        UIFactory.Stretch(abilityKey.rectTransform);

        // --- top: score / wave / gold ---
        scoreText = UIFactory.Label(root, "Score 0", 30, TextAlignmentOptions.Left);
        Place(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(400f, 40f));

        waveText = UIFactory.Label(root, "Wave 1", 34, TextAlignmentOptions.Center);
        Place(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 44f));

        killsText = UIFactory.Label(root, "Kills 0", 22, TextAlignmentOptions.Center);
        Place(killsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(300f, 30f));

        goldText = UIFactory.Label(root, "0 gold", 30, TextAlignmentOptions.Right);
        Place(goldText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(400f, 40f));

        // --- centre message (wave announcements) ---
        centerText = UIFactory.Label(root, "", 80, TextAlignmentOptions.Center);
        Place(centerText.rectTransform, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(1400f, 160f));
        centerText.fontStyle = FontStyles.Bold;
        centerText.color = new Color(1f, 1f, 1f, 0f);

        intermissionText = UIFactory.Label(root, "", 30, TextAlignmentOptions.Center);
        Place(intermissionText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(900f, 40f));

        // --- boss health bar ---
        bossBarObject = UIFactory.Box(root, "BossBar", new Color(0f, 0f, 0f, 0.6f)).gameObject;
        Place((RectTransform)bossBarObject.transform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1000f, 36f));
        bossBar = UIFactory.Box(bossBarObject.transform, "Fill", Palette.BossSkin);
        UIFactory.Stretch(bossBar.rectTransform, 3f);
        bossBar.type = Image.Type.Filled;
        bossBar.fillMethod = Image.FillMethod.Horizontal;
        bossNameText = UIFactory.Label(bossBarObject.transform, "Boss", 22, TextAlignmentOptions.Center);
        UIFactory.Stretch(bossNameText.rectTransform);
        bossBarObject.SetActive(false);

        // --- crosshair ---
        Image crosshair = UIFactory.Box(root, "Crosshair", new Color(1f, 1f, 1f, 0.5f));
        Place(crosshair.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    void Update()
    {
        // fade the big centre message (wave announcements) in and out
        if (centerTimer > 0f)
        {
            centerTimer -= Time.deltaTime;
            float fadeIn = (2f - centerTimer) / 0.3f;
            float fadeOut = centerTimer / 0.5f;
            float alpha = Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut));
            centerText.text = centerMessage;
            centerText.color = new Color(centerColor.r, centerColor.g, centerColor.b, alpha);
        }

        // keep the live bars (stamina / ability / mana) in sync each frame.
        // Look the player up once and remember it instead of searching every frame.
        if (cachedPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                cachedPlayer = playerObject.GetComponent<PlayerController>();
            }
        }

        PlayerController player = cachedPlayer;
        if (player == null)
        {
            return;
        }

        staminaBar.fillAmount = player.StaminaFraction;
        abilityFill.fillAmount = Mathf.Clamp01(player.AbilityFraction);

        Mage mage = player as Mage;
        if (mage != null)
        {
            manaBar.fillAmount = mage.ManaFraction;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsPlaying &&
            WaveManager.Instance != null && !bossBarObject.activeSelf)
        {
            waveText.text = "Wave " + WaveManager.Instance.CurrentWave + " / " + WaveManager.Instance.TotalWaves +
                            "   (" + WaveManager.Instance.EnemiesAlive + " left)";
        }
    }

    // --- Set* methods used by other scripts ---
    public void SetHealth(int current, int max)
    {
        healthBar.fillAmount = max > 0 ? (float)current / max : 0f;
        healthText.text = current + " / " + max;
        ScreenEffects.SetLowHealth(current > 0 && current < max * 0.3f);
    }

    public void SetGold(int gold)
    {
        goldText.text = gold + " gold";
    }

    public void SetScore(int score)
    {
        scoreText.text = "Score " + score;
    }

    public void SetKills(int kills)
    {
        killsText.text = "Kills " + kills;
    }

    public void ShowPlayerInfo(PlayerController player)
    {
        if (player == null)
        {
            return;
        }
        nameText.text = AuthManager.CurrentUsername + "  -  " + player.ClassName
            + (DatabaseManager.OfflineMode ? "   (offline)" : "");
        manaBar.transform.parent.gameObject.SetActive(player is Mage);
        SetHealth(player.CurrentHealth, player.MaxHealth);
        SetGold(player.Gold);
    }

    public void AnnounceWave(int wave, int total, bool boss)
    {
        ShowCenterMessage(boss ? "BOSS WAVE" : "WAVE " + wave, boss ? Palette.BossSkin : Color.white);
    }

    public void ShowWaveCleared(int wave, int bonus)
    {
        ShowCenterMessage("WAVE " + wave + " CLEARED", UIFactory.Accent);
    }

    void ShowCenterMessage(string message, Color color)
    {
        centerMessage = message;
        centerColor = color;
        centerTimer = 2f;
    }

    public void SetIntermission(int secondsLeft)
    {
        intermissionText.text = secondsLeft > 0
            ? "Next wave in " + secondsLeft + " seconds  -  press F to skip"
            : "";
    }


    public void ShowBossBar(string bossName, int maxHealth)
    {
        bossMaxHealth = Mathf.Max(1, maxHealth);
        bossNameText.text = bossName;
        bossBar.fillAmount = 1f;
        bossBarObject.SetActive(true);
    }

    public void SetBossHealth(int health)
    {
        bossBar.fillAmount = Mathf.Clamp01((float)health / bossMaxHealth);
    }

    public void HideBossBar()
    {
        bossBarObject.SetActive(false);
    }
}
