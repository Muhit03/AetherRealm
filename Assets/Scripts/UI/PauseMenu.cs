using UnityEngine;
using AetherRealm;

/// <summary>Esc-toggled pause overlay. Freezes time while open.</summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    bool _open;

    void Awake() => Instance = this;

    public void Build()
    {
        var root = (RectTransform)transform;
        UIFactory.Stretch(root);
        var dim = UIFactory.Box(root, "Dim", new Color(0f, 0f, 0f, 0.7f));
        UIFactory.Stretch(dim.rectTransform);

        var title = UIFactory.Label(root, "PAUSED", 90, TMPro.TextAlignmentOptions.Center);
        UIFactory.At(title.rectTransform, new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(800f, 140f));
        title.color = UIFactory.Accent;

        Btn(root, "RESUME", 40f, Toggle);
        Btn(root, "RESTART RUN", -60f, () => { SetOpen(false); GameManager.Instance.Restart(); });
        Btn(root, "MAIN MENU", -160f, () => { SetOpen(false); GameManager.Instance.QuitToMenu(); });

        gameObject.SetActive(false);
    }

    void Btn(RectTransform root, string label, float y, System.Action onClick)
    {
        var b = UIFactory.Button(root, label, onClick, new Vector2(420f, 76f));
        UIFactory.At((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(420f, 76f));
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsGameOver) return;
        if (!GameManager.Instance.IsPlaying && !_open) return;
        Toggle();
    }

    public void Toggle() => SetOpen(!_open);

    void SetOpen(bool on)
    {
        _open = on;
        gameObject.SetActive(on);
        Time.timeScale = on ? 0f : 1f;
    }
}
