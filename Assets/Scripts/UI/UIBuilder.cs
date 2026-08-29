using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherRealm
{
    /// <summary>
    /// Assembles the entire user interface at runtime: an EventSystem, one
    /// screen-space canvas and every panel with its controller component. Called
    /// once by <see cref="GameBootstrap"/>.
    /// </summary>
    public static class UIBuilder
    {
        public static void Build()
        {
            EnsureEventSystem();

            var canvas = UIFactory.Screen("GameCanvas", 100);
            var ui = canvas.GetComponent<UIManager>() ?? canvas.gameObject.AddComponent<UIManager>();
            if (UIManager.Instance == null) { /* Awake will set it */ }

            ui.hudPanel        = Panel<HUDController>(canvas.transform, "HUD", p => { });
            ui.mainMenuPanel   = Panel<MainMenuPanel>(canvas.transform, "MainMenu", p => p.Build());
            ui.loginPanel      = Panel<LoginPanel>(canvas.transform, "Login", p => p.Build());
            ui.leaderboardPanel= Panel<LeaderboardPanel>(canvas.transform, "Leaderboard", p => p.Build());
            ui.shopPanel       = Panel<ShopPanel>(canvas.transform, "Shop", p => p.Build());
            ui.pausePanel      = Panel<PauseMenu>(canvas.transform, "Pause", p => p.Build());
            ui.dialoguePanel   = Panel<DialoguePanel>(canvas.transform, "Dialogue", p => p.Build());

            var end = new GameObject("End", typeof(RectTransform));
            end.transform.SetParent(canvas.transform, false);
            ui.endScreen = end.AddComponent<EndScreen>();
            ui.endScreen.Build();
            ui.endPanel = end;

            ui.ShowMainMenu();
            ScreenEffects.Create();
            ScreenEffects.FadeIn();
        }

        static GameObject Panel<T>(Transform parent, string name, System.Action<T> build) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var comp = go.AddComponent<T>();
            build?.Invoke(comp);
            return go;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
