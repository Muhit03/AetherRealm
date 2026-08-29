using TMPro;
using UnityEngine;

namespace AetherRealm
{
    /// <summary>
    /// TextMesh Pro needs a font asset and this project has none imported. We try
    /// every reasonable source and, as a last resort, build one at runtime from
    /// an installed OS font so the UI always renders.
    /// </summary>
    public static class Fonts
    {
        static TMP_FontAsset _default;
        static bool _tried;

        public static TMP_FontAsset Default
        {
            get
            {
                if (_tried) return _default;
                _tried = true;
                _default = Resolve();
                if (_default == null)
                    Debug.LogWarning("AetherRealm: no font asset could be created; UI text may be invisible. " +
                                     "Import 'Window > TextMeshPro > Import TMP Essential Resources' to fix.");
                return _default;
            }
        }

        static TMP_FontAsset Resolve()
        {
            try { if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset; }
            catch { }

            foreach (var path in new[]
                     {
                         "LiberationSans SDF", "Fonts & Materials/LiberationSans SDF",
                         "Fonts & Materials/LegacyRuntime SDF",
                     })
            {
                var loaded = Resources.Load<TMP_FontAsset>(path);
                if (loaded != null) return loaded;
            }

            Font os = null;
            try
            {
                os = Font.CreateDynamicFontFromOSFont(
                    new[] { "Segoe UI", "Arial", "Tahoma", "Verdana", "Helvetica", "Liberation Sans", "DejaVu Sans" }, 40);
            }
            catch { }

            if (os == null)
            {
                try { os = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            }
            if (os == null)
            {
                try { os = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }

            if (os != null)
            {
                try { return TMP_FontAsset.CreateFontAsset(os); } catch { }
            }
            return null;
        }

        public static void Apply(TMP_Text label)
        {
            if (label != null && Default != null) label.font = Default;
        }
    }
}
