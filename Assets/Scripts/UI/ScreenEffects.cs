using UnityEngine;
using UnityEngine.UI;

namespace AetherRealm
{
    // Full-screen colour overlays: a red flash when the player is hit, a red
    // edge when health is low, and a black fade for starting/restarting.
    // Creates its own canvas the first time it is used.
    public class ScreenEffects : MonoBehaviour
    {
        static ScreenEffects instance;

        Image hurtImage;
        Image lowHealthImage;
        Image fadeImage;

        float hurtAlpha;
        bool lowHealth;
        float fadeAlpha = 1f;
        float fadeTarget;

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
            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            lowHealthImage = MakeFullScreenImage(canvasObject.transform, new Color(0.6f, 0f, 0f, 0f));
            hurtImage = MakeFullScreenImage(canvasObject.transform, new Color(0.8f, 0f, 0f, 0f));
            fadeImage = MakeFullScreenImage(canvasObject.transform, new Color(0f, 0f, 0f, 1f));
        }

        static Image MakeFullScreenImage(Transform parent, Color color)
        {
            GameObject go = new GameObject("Overlay");
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
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

            hurtAlpha = Mathf.MoveTowards(hurtAlpha, 0f, deltaTime * 2f);
            hurtImage.color = new Color(0.8f, 0f, 0f, hurtAlpha * 0.5f);

            float lowHealthTarget = lowHealth ? 0.3f : 0f;
            Color c = lowHealthImage.color;
            c.a = Mathf.MoveTowards(c.a, lowHealthTarget, deltaTime * 0.5f);
            lowHealthImage.color = c;

            fadeAlpha = Mathf.MoveTowards(fadeAlpha, fadeTarget, deltaTime / 0.6f);
            fadeImage.color = new Color(0f, 0f, 0f, fadeAlpha);
        }

        public static void FlashHurt() { Instance.hurtAlpha = 1f; }
        public static void SetLowHealth(bool on) { Instance.lowHealth = on; }
        public static void FadeIn() { Instance.fadeTarget = 0f; }
        public static void FadeOut() { Instance.fadeTarget = 1f; }
        public static void Create() { var unused = Instance; }
    }
}
