using UnityEngine;

namespace AetherRealm
{
    // A wall torch: a warm light that flickers and a small glowing flame.
    public class Torch : MonoBehaviour
    {
        Light torchLight;
        float baseIntensity;
        float noiseSeed;

        public static Torch Create(Vector3 position)
        {
            GameObject go = new GameObject("Torch");
            go.transform.position = position;

            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame";
            Destroy(flame.GetComponent<Collider>());
            flame.transform.SetParent(go.transform, false);
            flame.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
            flame.GetComponent<Renderer>().sharedMaterial = Palette.GlowMaterial(Palette.Flame);

            Light light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.3f);
            light.range = 9f;
            light.intensity = 2.5f;

            Torch torch = go.AddComponent<Torch>();
            torch.torchLight = light;
            torch.baseIntensity = light.intensity;
            torch.noiseSeed = Random.value * 100f;
            return torch;
        }

        void Update()
        {
            if (torchLight == null)
            {
                return;
            }

            float flicker = Mathf.PerlinNoise(noiseSeed, Time.time * 6f);
            torchLight.intensity = baseIntensity * (0.75f + flicker * 0.5f);
        }
    }
}
