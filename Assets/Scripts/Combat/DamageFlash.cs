using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AetherRealm
{
    // Flashes a character white for a moment when it gets hurt, then puts the
    // original materials back.
    public class DamageFlash : MonoBehaviour
    {
        List<Renderer> renderers = new List<Renderer>();
        List<Material[]> originalMaterials = new List<Material[]>();
        Material flashMaterial;
        bool ready;

        public void Setup(List<Renderer> characterRenderers, Color flashColor)
        {
            renderers.Clear();
            originalMaterials.Clear();

            foreach (Renderer renderer in characterRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }
                renderers.Add(renderer);
                originalMaterials.Add(renderer.sharedMaterials);
            }

            flashMaterial = Palette.UnlitMaterial(flashColor);
            ready = true;
        }

        public void Flash()
        {
            if (!ready || renderers.Count == 0)
            {
                return;
            }
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }
                Material[] flash = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < flash.Length; i++)
                {
                    flash[i] = flashMaterial;
                }
                renderer.sharedMaterials = flash;
            }

            yield return new WaitForSeconds(0.08f);

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sharedMaterials = originalMaterials[i];
                }
            }
        }
    }
}
