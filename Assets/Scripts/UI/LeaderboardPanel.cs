using System.Collections.Generic;
using TMPro;
using UnityEngine;
using AetherRealm;

/// <summary>
/// Shows the top scores pulled from SQL Server via <see cref="LeaderboardManager"/>.
/// Toggled with Tab. Builds its rows in code.
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    public static LeaderboardPanel Instance { get; private set; }

    Transform _rows;
    readonly List<GameObject> _spawned = new List<GameObject>();

    void Awake() => Instance = this;

    public void Build()
    {
        var root = (RectTransform)transform;
        UIFactory.Stretch(root);
        var dim = UIFactory.Box(root, "Dim", new Color(0f, 0f, 0f, 0.6f));
        UIFactory.Stretch(dim.rectTransform);

        var card = UIFactory.Box(root, "Card", UIFactory.Panel);
        UIFactory.At(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 820f));

        var title = UIFactory.Label(card.transform, "LEADERBOARD", 46, TextAlignmentOptions.Center);
        UIFactory.At(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(900f, 56f));
        title.color = UIFactory.Accent;

        _rows = UIFactory.Rect("Rows", card.transform);
        UIFactory.At((RectTransform)_rows, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1000f, 620f));

        var hint = UIFactory.Label(card.transform, "press Tab to close", 24, TextAlignmentOptions.Center);
        UIFactory.At(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(600f, 30f));

        gameObject.SetActive(false);
    }

    public void Populate()
    {
        foreach (var g in _spawned) if (g != null) Destroy(g);
        _spawned.Clear();

        LeaderboardManager.Instance?.Refresh();
        var entries = LeaderboardManager.Instance != null
            ? LeaderboardManager.Instance.GetCachedEntries()
            : new List<LeaderboardEntry>();

        Row("#", "Player", "Class", "Score", "Kills", "Time", true);

        if (entries.Count == 0)
        {
            Row("", "No scores recorded yet - be the first!", "", "", "", "", false);
            return;
        }

        foreach (var e in entries)
            Row(e.Rank.ToString(), e.Username, e.ClassType, e.Score.ToString(),
                e.Kills.ToString(), Time(e.PlayTimeSecs), false);
    }

    void Row(string rank, string name, string cls, string score, string kills, string time, bool header)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(_rows, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -_spawned.Count * 46f);
        rt.sizeDelta = new Vector2(0f, 42f);

        string[] cells = { rank, name, cls, score, kills, time };
        float[] x = { 0f, 90f, 480f, 660f, 800f, 900f };
        for (int i = 0; i < cells.Length; i++)
        {
            var label = UIFactory.Label(go.transform, cells[i], header ? 26 : 24,
                i == 1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Left);
            UIFactory.At(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(x[i], 0f), new Vector2(400f, 40f));
            if (header) label.color = UIFactory.Accent;
        }

        _spawned.Add(go);
    }

    static string Time(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";
}
