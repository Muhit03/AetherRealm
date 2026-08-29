using UnityEngine;
using UnityEngine.UI;

namespace AetherRealm
{
    // Full-screen overlays, kept deliberately subtle:
    //  - a quick faint red flash when the player is hit
    //  - a faint red pulse while health is low
    //  - a black fade for starting / restarting
    // Lives on its own canvas UNDER the main UI so it never tints the HUD text.
    public class ScreenEffects : MonoBehaviour
    {
        static ScreenEffects instance;

        Image redImage;    // hit flash + low-health pulse (behind the HUD)
        Image fadeImage;    // black fade (in front of everything)

        float hitAmount;
        bool lowHealth;
        float fadeAmount = 1f;
        float fadeTarget;
        float pulse;

        static ScreenEffects Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("ScreenEffects");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<ScreenEffects>();
                    instance.Build();
                }
                return instance;
            }
        }

        void Build()
        {
            // red overlay sits behind the HUD (order 90 < GameCanvas 100)
            redImage = MakeOverlay("RedCanvas", 90);
            redImage.color = new Color(0.7f, 0.05f, 0.05f, 0f);

            // black fade sits on top of everything
            fadeImage = MakeOverlay("FadeCanvas", 950);
            fadeImage.color = new Color(0f, 0f, 0f, 1f);
        }

        Image MakeOverlay(string name, int order)
        {
            GameObject canvasObject = new GameObject(name);
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;

            GameObject imageObject = new GameObject("Overlay");
            imageObject.transform.SetParent(canvasObject.transform, false);
            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            pulse += deltaTime * 3f;

            hitAmount = Mathf.MoveTowards(hitAmount, 0f, deltaTime * 6f);

            float lowPulse = lowHealth ? (0.09f + 0.04f * Mathf.Sin(pulse)) : 0f;
            float red = Mathf.Max(hitAmount * 0.13f, lowPulse);
            redImage.color = new Color(0.7f, 0.05f, 0.05f, red);

            fadeAmount = Mathf.MoveTowards(fadeAmount, fadeTarget, deltaTime / 0.6f);
            fadeImage.color = new Color(0f, 0f, 0f, fadeAmount);
        }

        // for the CI diagnostic
        public string DebugState()
        {
            return "redA=" + redImage.color.a.ToString("F2") +
                   " fadeA=" + fadeImage.color.a.ToString("F2") +
                   " lowHealth=" + lowHealth + " hitAmount=" + hitAmount.ToString("F2");
        }

        public static string Debug()
        {
            return instance != null ? instance.DebugState() : "(no ScreenEffects)";
        }

        public static void FlashHurt() { if (Instance.hitAmount < 0.3f) Instance.hitAmount = 1f; }
        public static void SetLowHealth(bool on) { Instance.lowHealth = on; }
        public static void FadeIn() { Instance.fadeTarget = 0f; }
        public static void FadeOut() { Instance.fadeTarget = 1f; }
        public static void Create() { var unused = Instance; }
    }
}
