using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AetherRealm;

/// <summary>
/// The shop Elder Eldrin opens between waves. Gold earned from kills buys
/// permanent upgrades for the rest of the run.
/// </summary>
public class ShopPanel : MonoBehaviour
{
    public static ShopPanel Instance { get; private set; }

    // id, name, description, base cost. The id must match the ones GameManager
    // reads in ApplyUpgrades().
    class Upgrade
    {
        public string id;
        public string name;
        public string description;
        public int baseCost;

        public Upgrade(string id, string name, string description, int baseCost)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.baseCost = baseCost;
        }
    }

    List<Upgrade> upgrades = new List<Upgrade>
    {
        new Upgrade("health",    "Vitality",  "+25 max health",        60),
        new Upgrade("damage",    "Sharpen",   "+6 attack damage",      70),
        new Upgrade("speed",     "Swiftness", "+8% move speed",        55),
        new Upgrade("cooldown",  "Focus",     "-15% ability cooldown", 80),
        new Upgrade("lifesteal", "Vampirism", "+6% lifesteal",         90),
    };

    List<TMP_Text> rowLabels = new List<TMP_Text>();
    List<Button> rowButtons = new List<Button>();
    TMP_Text goldLabel;

    void Awake()
    {
        Instance = this;
    }

    public void Build()
    {
        RectTransform root = (RectTransform)transform;
        UIFactory.Stretch(root);
        Image dim = UIFactory.Box(root, "Dim", new Color(0f, 0f, 0f, 0.55f));
        UIFactory.Stretch(dim.rectTransform);

        Image card = UIFactory.Box(root, "Card", UIFactory.Panel);
        UIFactory.At(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 760f));

        TMP_Text title = UIFactory.Label(card.transform, "ELDER ELDRIN'S WARES", 42, TextAlignmentOptions.Center);
        UIFactory.At(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(820f, 56f));
        title.color = UIFactory.Accent;

        goldLabel = UIFactory.Label(card.transform, "Gold: 0", 30, TextAlignmentOptions.Center);
        UIFactory.At(goldLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(820f, 40f));

        for (int i = 0; i < upgrades.Count; i++)
        {
            int index = i; // capture for the button callback

            Image row = UIFactory.Box(card.transform, "Row", new Color(0.12f, 0.13f, 0.18f, 1f));
            UIFactory.At(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f - i * 96f), new Vector2(820f, 84f));

            TMP_Text label = UIFactory.Label(row.transform, "", 24, TextAlignmentOptions.Left);
            UIFactory.At(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(560f, 76f));
            rowLabels.Add(label);

            Button buyButton = UIFactory.Button(row.transform, "BUY", delegate { Buy(index); }, new Vector2(180f, 60f));
            UIFactory.At((RectTransform)buyButton.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(180f, 60f));
            rowButtons.Add(buyButton);
        }

        Button doneButton = UIFactory.Button(card.transform, "NEXT WAVE  [F]", delegate
        {
            if (WaveManager.Instance != null) WaveManager.Instance.SkipShopBreak();
        }, new Vector2(500f, 70f));
        UIFactory.At((RectTransform)doneButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(500f, 70f));

        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    int CostOf(int index)
    {
        int level = (GameManager.Instance != null) ? GameManager.Instance.GetUpgradeLevel(upgrades[index].id) : 0;
        return upgrades[index].baseCost * (level + 1);
    }

    void Buy(int index)
    {
        PlayerController player = FindPlayer();
        if (player == null || GameManager.Instance == null)
        {
            return;
        }

        int cost = CostOf(index);
        if (player.SpendGold(cost))
        {
            GameManager.Instance.BuyUpgrade(upgrades[index].id);
            AudioManager.Play(AudioManager.Sound.Buy);
        }
        Refresh();
    }

    void Refresh()
    {
        PlayerController player = FindPlayer();
        int gold = (player != null) ? player.Gold : 0;
        goldLabel.text = "Gold: " + gold;

        for (int i = 0; i < upgrades.Count; i++)
        {
            int level = (GameManager.Instance != null) ? GameManager.Instance.GetUpgradeLevel(upgrades[i].id) : 0;
            int cost = CostOf(i);
            rowLabels[i].text = upgrades[i].name + " (Level " + level + ")\n<size=20>" + upgrades[i].description + " - " + cost + " gold</size>";

            bool canAfford = gold >= cost;
            TMP_Text buttonText = rowButtons[i].GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = canAfford ? "BUY" : "NO GOLD";
            }
        }
    }

    PlayerController FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return (playerObject != null) ? playerObject.GetComponent<PlayerController>() : null;
    }
}
