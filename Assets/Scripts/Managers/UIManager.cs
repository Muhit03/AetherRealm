using UnityEngine;
using AetherRealm;

/// <summary>
/// Switches between the game's full-screen panels. The panels themselves are
/// assembled in code by <see cref="UIBuilder"/>, which fills in these fields.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [HideInInspector] public GameObject mainMenuPanel;
    [HideInInspector] public GameObject loginPanel;
    [HideInInspector] public GameObject hudPanel;
    [HideInInspector] public GameObject leaderboardPanel;
    [HideInInspector] public GameObject pausePanel;
    [HideInInspector] public GameObject shopPanel;
    [HideInInspector] public GameObject endPanel;
    [HideInInspector] public GameObject dialoguePanel;

    [HideInInspector] public EndScreen endScreen;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Set(GameObject go, bool on) { if (go != null) go.SetActive(on); }

    public void ShowMainMenu()
    {
        Set(mainMenuPanel, true);
        Set(loginPanel, false);
        Set(hudPanel, false);
        Set(leaderboardPanel, false);
        Set(pausePanel, false);
        Set(shopPanel, false);
        Set(endPanel, false);
    }

    public void ShowLoginPanel()
    {
        Set(mainMenuPanel, false);
        Set(loginPanel, true);
    }

    public void ShowHUD()
    {
        Set(mainMenuPanel, false);
        Set(loginPanel, false);
        Set(endPanel, false);
        Set(hudPanel, true);
    }

    public void ToggleLeaderboard()
    {
        if (leaderboardPanel == null) return;
        bool on = !leaderboardPanel.activeSelf;
        leaderboardPanel.SetActive(on);
        if (on)
        {
            leaderboardPanel.transform.SetAsLastSibling();
            LeaderboardPanel.Instance?.Populate();
        }
    }

    public void ShowEndScreen(bool victory, int score, int kills, int wave)
    {
        Set(hudPanel, true); // keep HUD visible faintly behind
        Set(shopPanel, false);
        Set(pausePanel, false);
        Set(endPanel, true);
        endScreen?.Show(victory, score, kills, wave);
    }

    // kept for backwards compatibility with older callers
    public void ShowGameOver(int score, int kills) => ShowEndScreen(false, score, kills, 0);
}
