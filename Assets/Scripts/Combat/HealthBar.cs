using UnityEngine;

namespace AetherRealm
{
    // A small health bar that floats above an enemy and always faces the camera.
    // Made from two flat quads so it doesn't need a UI canvas.
    public class HealthBar : MonoBehaviour
    {
        Transform fill;
        float shownAmount = 1f;
        float targetAmount = 1f;

        public static HealthBar Attach(Transform owner, float height)
        {
            GameObject root = new GameObject("HealthBar");
            root.transform.SetParent(owner, false);
            root.transform.localPosition = Vector3.up * height;

            MakeQuad("Background", root.transform, new Vector3(1.1f, 0.16f, 1f), new Color(0f, 0f, 0f, 0.7f), 0f);
            Transform fillQuad = MakeQuad("Fill", root.transform, new Vector3(1f, 0.11f, 1f), Palette.Health, -0.01f);

            HealthBar bar = root.AddComponent<HealthBar>();
            bar.fill = fillQuad;
            return bar;
        }

        static Transform MakeQuad(string name, Transform parent, Vector3 scale, Color color, float z)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(0f, 0f, z);
            quad.transform.localScale = scale;
            quad.GetComponent<Renderer>().sharedMaterial = Palette.UnlitMaterial(color);
            return quad.transform;
        }

        public void SetAmount(float amount)
        {
            targetAmount = Mathf.Clamp01(amount);
        }

        void LateUpdate()
        {
            shownAmount = Mathf.MoveTowards(shownAmount, targetAmount, Time.deltaTime * 1.5f);

            // shrink the fill quad from the left
            fill.localScale = new Vector3(Mathf.Max(shownAmount, 0.001f), 0.11f, 1f);
            fill.localPosition = new Vector3(-(1f - shownAmount) / 2f, 0f, fill.localPosition.z);

            Camera camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }
        }
    }
}
