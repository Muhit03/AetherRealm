using TMPro;
using UnityEngine;
using AetherRealm;

/// <summary>Victory / defeat screen: the run summary and the buttons.</summary>
public class EndScreen : MonoBehaviour
{
    TMP_Text _title, _summary, _note;
    RectTransform _continueBtn, _againBtn, _boardBtn, _menuBtn;

    public void Build()
    {
        var root = (RectTransform)transform;
        UIFactory.Stretch(root);
        var dim = UIFactory.Box(root, "Dim", new Color(0f, 0f, 0f, 0.85f));
        UIFactory.Stretch(dim.rectTransform);

        _title = UIFactory.Label(root, "", 88, TextAlignmentOptions.Center);
        UIFactory.At(_title.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(1400f, 130f));
        _title.fontStyle = FontStyles.Bold;

        // stats block: top-aligned so it always grows downward, clear of the title
        _summary = UIFactory.Label(root, "", 36, TextAlignmentOptions.Top);
        UIFactory.At(_summary.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(1100f, 200f));

        _note = UIFactory.Label(root, "", 26, TextAlignmentOptions.Center);
        UIFactory.At(_note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 55f), new Vector2(1200f, 40f));
        _note.color = new Color(0.6f, 0.85f, 0.6f);

        _continueBtn = Btn(root, "CONTINUE", () => GameManager.Instance.ContinueFromCheckpoint());
        _againBtn    = Btn(root, "PLAY AGAIN", () => GameManager.Instance.Restart());
        _boardBtn    = Btn(root, "LEADERBOARD", () => UIManager.Instance.ToggleLeaderboard());
        _menuBtn     = Btn(root, "MAIN MENU", () => GameManager.Instance.QuitToMenu());

        gameObject.SetActive(false);
    }

    RectTransform Btn(RectTransform root, string label, System.Action onClick)
    {
        var b = UIFactory.Button(root, label, onClick, new Vector2(460f, 78f));
        return (RectTransform)b.transform;
    }

    void Place(RectTransform b, float y)
    {
        UIFactory.At(b, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(460f, 78f));
    }

    public void Show(bool victory, int score, int kills, int wavesCleared)
    {
        gameObject.SetActive(true);
        _title.text = victory ? "VICTORY" : "YOU FELL";
        _title.color = victory ? UIFactory.Accent : new Color(0.85f, 0.3f, 0.3f);

        int seconds = GameManager.Instance != null ? GameManager.Instance.SecondsPlayed : 0;
        int damage = GameManager.Instance != null ? GameManager.Instance.DamageDealt : 0;

        _summary.text =
            $"Final Score   <b>{score}</b>\n" +
            $"Waves Cleared   <b>{wavesCleared}</b>          Kills   <b>{kills}</b>\n" +
            $"Damage Dealt   <b>{damage}</b>          Time   <b>{seconds / 60:00}:{seconds % 60:00}</b>";

        // a checkpoint only exists if the player fell (not on a win) past wave 1
        bool canContinue = !victory && RunSave.Has();
        _continueBtn.gameObject.SetActive(canContinue);

        if (canContinue)
        {
            _note.text = "Checkpoint at Wave " + RunSave.SavedWave() +
                         " - continue now, or later from the main menu.";
        }
        else
        {
            _note.text = AuthManager.IsLoggedIn ? "Run saved to the leaderboard." : "";
        }

        // stack the buttons top-down, leaving out CONTINUE when it is hidden
        float y = -35f;
        if (canContinue) { Place(_continueBtn, y); y -= 96f; }
        Place(_againBtn, y); y -= 96f;
        Place(_boardBtn, y); y -= 96f;
        Place(_menuBtn, y);
    }
}
