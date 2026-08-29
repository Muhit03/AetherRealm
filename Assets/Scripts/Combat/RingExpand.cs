using UnityEngine;

namespace AetherRealm
{
    // Grows a flat disc outward and then removes it. Used for the shockwave of
    // the Warrior's shield bash and the boss's ground slam.
    public class RingExpand : MonoBehaviour
    {
        float targetSize;
        float duration;
        float timer;

        public void Play(float finalDiameter, float seconds)
        {
            targetSize = finalDiameter;
            duration = seconds;
        }

        void Update()
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            float size = Mathf.Lerp(0.4f, targetSize, t);
            transform.localScale = new Vector3(size, 0.05f, size);

            if (timer >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
