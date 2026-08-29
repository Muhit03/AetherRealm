using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AetherRealm;

/// <summary>Title screen: shows the game name and a few buttons.</summary>
public class MainMenuPanel : MonoBehaviour
{
    TMP_Text _tip;

    public void Build()
    {
        var root = (RectTransform)transform;
        UIFactory.Stretch(root);
        var bg = UIFactory.Box(root, "BG", new Color(0.04f, 0.05f, 0.08f, 1f));
        UIFactory.Stretch(bg.rectTransform);

        var title = UIFactory.Label(root, "AETHERREALM", 130, TextAlignmentOptions.Center);
        UIFactory.At(title.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1600f, 200f));
        title.fontStyle = FontStyles.Bold;
        title.color = UIFactory.Accent;

        var sub = UIFactory.Label(root, "Arena of the Fallen Star", 40, TextAlignmentOptions.Center);
        UIFactory.At(sub.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1200f, 60f));

        MenuButton(root, "ENTER THE ARENA", 0f, () => UIManager.Instance.ShowLoginPanel());
        MenuButton(root, "HOW TO PLAY", -104f, ToggleTips);
        MenuButton(root, "QUIT", -196f, Quit);

        _tip = UIFactory.Label(root, TipText(), 30, TextAlignmentOptions.Center);
        UIFactory.At(_tip.rectTransform, new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(1400f, 240f));
        _tip.gameObject.SetActive(false);
    }

    static void MenuButton(RectTransform root, string label, float y, System.Action onClick)
    {
        var b = UIFactory.Button(root, label, onClick, new Vector2(460f, 80f));
        UIFactory.At((RectTransform)b.transform, new Vector2(0.5f, 0.42f), new Vector2(0f, y), new Vector2(460f, 80f));
    }

    static string TipText() =>
        "WASD - move        Left Mouse - attack        Space - dodge roll\n" +
        "Q / Right Mouse - class ability        E - talk / interact        Tab - leaderboard\n" +
        "Esc - pause        F - skip the shop break\n\n" +
        "Survive 8 waves. Spend gold at Elder Eldrin between waves. Beat the Ogre Warlord.";

    void ToggleTips() { if (_tip != null) _tip.gameObject.SetActive(!_tip.gameObject.activeSelf); }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
