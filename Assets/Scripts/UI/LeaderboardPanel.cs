using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AetherRealm;

/// <summary>
/// Shows every registered player's best stats, pulled from SQL Server (or the
/// local fallback) via <see cref="LeaderboardManager"/>. The buttons at the top
/// re-sort the table by score, waves, kills or damage. Toggled with Tab.
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    public static LeaderboardPanel Instance { get; private set; }

    Transform rowsParent;
    string sortBy = "score";
    readonly List<GameObject> spawnedRows = new List<GameObject>();
    readonly List<Button> sortButtons = new List<Button>();
    readonly string[] sortKeys = { "score", "waves", "kills", "damage" };
    readonly string[] sortLabels = { "SCORE", "WAVES", "KILLS", "DAMAGE" };

    void Awake()
    {
        Instance = this;
    }

    public void Build()
    {
        RectTransform root = (RectTransform)transform;
        UIFactory.Stretch(root);
        Image dim = UIFactory.Box(root, "Dim", new Color(0f, 0f, 0f, 0.6f));
        UIFactory.Stretch(dim.rectTransform);

        Image card = UIFactory.Box(root, "Card", UIFactory.Panel);
        UIFactory.At(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 840f));

        TMP_Text title = UIFactory.Label(card.transform, "LEADERBOARD", 44, TextAlignmentOptions.Center);
        UIFactory.At(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(900f, 52f));
        title.color = UIFactory.Accent;

        // sort buttons
        for (int i = 0; i < sortKeys.Length; i++)
        {
            string key = sortKeys[i];
            Button button = UIFactory.Button(card.transform, sortLabels[i], delegate { SetSort(key); }, new Vector2(210f, 56f));
            UIFactory.At((RectTransform)button.transform, new Vector2(0.5f, 1f),
                new Vector2(-345f + i * 230f, -120f), new Vector2(210f, 56f));
            sortButtons.Add(button);
        }

        rowsParent = UIFactory.Rect("Rows", card.transform);
        UIFactory.At((RectTransform)rowsParent, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1080f, 600f));

        TMP_Text hint = UIFactory.Label(card.transform, "press Tab to close", 22, TextAlignmentOptions.Center);
        UIFactory.At(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(600f, 28f));

        gameObject.SetActive(false);
    }

    void SetSort(string key)
    {
        sortBy = key;
        Populate();
    }

    public void Populate()
    {
        foreach (GameObject go in spawnedRows)
        {
            if (go != null) Destroy(go);
        }
        spawnedRows.Clear();

        // highlight the active sort button
        for (int i = 0; i < sortButtons.Count; i++)
        {
            Image img = sortButtons[i].targetGraphic as Image;
            if (img != null)
            {
                img.color = sortKeys[i] == sortBy
                    ? UIFactory.Accent
                    : new Color(0.16f, 0.18f, 0.24f, 1f);
            }
        }

        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.Refresh(sortBy);
        }

        List<LeaderboardEntry> entries = LeaderboardManager.Instance != null
            ? LeaderboardManager.Instance.GetCachedEntries()
            : new List<LeaderboardEntry>();

        AddRow("#", "Player", "Class", "Score", "Waves", "Kills", "Damage", true);

        if (entries.Count == 0)
        {
            AddRow("", "No runs recorded yet - be the first!", "", "", "", "", "", false);
            return;
        }

        foreach (LeaderboardEntry e in entries)
        {
            AddRow(e.Rank.ToString(), e.Username, e.ClassType,
                e.Score.ToString(), e.Waves.ToString(), e.Kills.ToString(), e.Damage.ToString(), false);
        }
    }

    void AddRow(string rank, string name, string cls, string score, string waves, string kills, string damage, bool header)
    {
        GameObject go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(rowsParent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -spawnedRows.Count * 40f);
        rt.sizeDelta = new Vector2(0f, 38f);

        string[] cells = { rank, name, cls, score, waves, kills, damage };
        float[] columnX = { 0f, 70f, 470f, 640f, 760f, 880f, 1000f };

        for (int i = 0; i < cells.Length; i++)
        {
            TMP_Text label = UIFactory.Label(go.transform, cells[i], header ? 24 : 22, TextAlignmentOptions.Left);
            UIFactory.At(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(columnX[i], 0f), new Vector2(360f, 36f));
            if (header) label.color = UIFactory.Accent;
        }

        spawnedRows.Add(go);
    }
}
