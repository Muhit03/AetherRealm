using TMPro;
using UnityEngine;
using AetherRealm;

/// <summary>Victory / defeat screen with the run summary and leaderboard shortcut.</summary>
public class EndScreen : MonoBehaviour
{
    TMP_Text _title, _summary;

    public void Build()
    {
        var root = (RectTransform)transform;
        UIFactory.Stretch(root);
        var dim = UIFactory.Box(root, "Dim", new Color(0f, 0f, 0f, 0.8f));
        UIFactory.Stretch(dim.rectTransform);

        _title = UIFactory.Label(root, "", 110, TextAlignmentOptions.Center);
        UIFactory.At(_title.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1400f, 160f));
        _title.fontStyle = FontStyles.Bold;

        _summary = UIFactory.Label(root, "", 40, TextAlignmentOptions.Center);
        UIFactory.At(_summary.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 260f));

        Btn(root, "PLAY AGAIN", 40f, () => GameManager.Instance.Restart());
        Btn(root, "LEADERBOARD", -60f, () => UIManager.Instance.ToggleLeaderboard());
        Btn(root, "MAIN MENU", -160f, () => GameManager.Instance.QuitToMenu());

        gameObject.SetActive(false);
    }

    void Btn(RectTransform root, string label, float y, System.Action onClick)
    {
        var b = UIFactory.Button(root, label, onClick, new Vector2(420f, 76f));
        UIFactory.At((RectTransform)b.transform, new Vector2(0.5f, 0.42f), new Vector2(0f, y), new Vector2(420f, 76f));
    }

    public void Show(bool victory, int score, int kills, int wave)
    {
        gameObject.SetActive(true);
        _title.text = victory ? "VICTORY" : "YOU FELL";
        _title.color = victory ? UIFactory.Accent : new Color(0.85f, 0.3f, 0.3f);
        int t = GameManager.Instance != null ? GameManager.Instance.SecondsPlayed : 0;
        _summary.text =
            $"Final Score   <b>{score}</b>\n" +
            $"Kills   <b>{kills}</b>          Reached   <b>Wave {Mathf.Max(1, wave)}</b>\n" +
            $"Time Survived   <b>{t / 60:00}:{t % 60:00}</b>\n\n" +
            (AuthManager.IsLoggedIn ? "Score submitted to the leaderboard." : "");
    }
}
