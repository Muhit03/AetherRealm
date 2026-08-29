using System.Collections.Generic;
using UnityEngine;

namespace AetherRealm
{
    // The project has no material assets, so we create every material here in
    // code. Materials are stored in a dictionary and reused so we don't make
    // hundreds of copies of the same colour.
    public static class Palette
    {
        // World colours
        public static readonly Color Ground = new Color(0.14f, 0.15f, 0.19f);
        public static readonly Color Stone = new Color(0.34f, 0.35f, 0.40f);
        public static readonly Color StoneDark = new Color(0.20f, 0.21f, 0.25f);
        public static readonly Color Wood = new Color(0.33f, 0.21f, 0.12f);
        public static readonly Color Flame = new Color(1f, 0.55f, 0.16f);
        public static readonly Color Portal = new Color(0.60f, 0.28f, 0.98f);

        // Character colours
        public static readonly Color Skin = new Color(0.92f, 0.75f, 0.62f);
        public static readonly Color WarriorCloth = new Color(0.20f, 0.42f, 0.85f);
        public static readonly Color WarriorSteel = new Color(0.72f, 0.76f, 0.82f);
        public static readonly Color MageCloth = new Color(0.42f, 0.28f, 0.78f);
        public static readonly Color MageGlow = new Color(0.55f, 0.45f, 1f);
        public static readonly Color GoblinSkin = new Color(0.40f, 0.60f, 0.28f);
        public static readonly Color GoblinCloth = new Color(0.28f, 0.24f, 0.20f);
        public static readonly Color ArcherCloth = new Color(0.45f, 0.36f, 0.26f);
        public static readonly Color BossSkin = new Color(0.55f, 0.20f, 0.16f);
        public static readonly Color BossIron = new Color(0.30f, 0.30f, 0.33f);

        // UI / effect colours
        public static readonly Color Gold = new Color(1f, 0.80f, 0.30f);
        public static readonly Color Health = new Color(0.90f, 0.26f, 0.30f);
        public static readonly Color Mana = new Color(0.30f, 0.58f, 0.98f);
        public static readonly Color Stamina = new Color(0.45f, 0.85f, 0.45f);

        static Dictionary<string, Material> cache = new Dictionary<string, Material>();

        // A normal solid material that reacts to light.
        public static Material Material(Color color)
        {
            string key = "solid " + color;
            if (cache.ContainsKey(key))
            {
                return cache[key];
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            SetColor(material, color);
            cache[key] = material;
            return material;
        }

        // A material that glows (used for magic, portals, torches, coins).
        public static Material GlowMaterial(Color color)
        {
            string key = "glow " + color;
            if (cache.ContainsKey(key))
            {
                return cache[key];
            }

            Material material = Material(color);
            material = new Material(material);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * 2.2f);
            }

            cache[key] = material;
            return material;
        }

        // A flat colour that ignores lighting (used for UI bits shown in the world).
        public static Material UnlitMaterial(Color color)
        {
            string key = "unlit " + color;
            if (cache.ContainsKey(key))
            {
                return cache[key];
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            SetColor(material, color);
            cache[key] = material;
            return material;
        }

        static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
