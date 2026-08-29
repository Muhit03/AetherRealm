using System.Collections;
using UnityEngine;

namespace AetherRealm
{
    // Simple "game feel" helpers: shake the camera, freeze the game for a split
    // second on a big hit, show floating text, and throw a few sparks.
    // It creates itself the first time it is used so nothing needs wiring up.
    //
    // To keep things smooth during busy waves there are hard caps on how many
    // floating texts and sparks can exist at once.
    public class Effects : MonoBehaviour
    {
        const int MaxTexts = 18;
        const int MaxSparks = 70;

        static Effects instance;
        public static int LiveTexts;
        public static int LiveSparks;

        static Effects Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("Effects");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Effects>();
                }
                return instance;
            }
        }

        float shakeAmount;
        Vector3 currentShakeOffset;

        void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            // undo last frame's shake, then apply this frame's
            camera.transform.localPosition -= currentShakeOffset;

            if (shakeAmount > 0.001f)
            {
                currentShakeOffset = Random.insideUnitSphere * shakeAmount;
                shakeAmount = shakeAmount - Time.unscaledDeltaTime * 6f;
                if (shakeAmount < 0f)
                {
                    shakeAmount = 0f;
                }
            }
            else
            {
                currentShakeOffset = Vector3.zero;
            }

            camera.transform.localPosition += currentShakeOffset;
        }

        // Called when a new run starts, in case the counters drifted.
        public static void ResetCounters()
        {
            LiveTexts = 0;
            LiveSparks = 0;
        }

        public static void Shake(float amount)
        {
            if (amount > Instance.shakeAmount)
            {
                Instance.shakeAmount = amount;
            }
        }

        Coroutine freezeRoutine;

        public static void FreezeFrame(float seconds)
        {
            // never hit-stop while the game is paused
            if (Time.timeScale < 0.01f)
            {
                return;
            }
            if (Instance.freezeRoutine != null)
            {
                Instance.StopCoroutine(Instance.freezeRoutine);
            }
            Instance.freezeRoutine = Instance.StartCoroutine(Instance.FreezeRoutine(seconds));
        }

        IEnumerator FreezeRoutine(float seconds)
        {
            Time.timeScale = 0.15f;
            yield return new WaitForSecondsRealtime(Mathf.Min(seconds, 0.08f));
            Time.timeScale = 1f;
            freezeRoutine = null;
        }

        void Update()
        {
            // safety net: if we somehow got left in slow motion with no freeze
            // running and the game isn't paused, snap back to normal speed.
            if (freezeRoutine == null && Time.timeScale > 0.02f && Time.timeScale < 0.95f)
            {
                Time.timeScale = 1f;
            }
        }

        public static void FloatingLabel(Vector3 position, string message, Color color, float size)
        {
            if (LiveTexts >= MaxTexts)
            {
                return;
            }
            GameObject go = new GameObject("FloatingText");
            go.transform.position = position;
            FloatingText text = go.AddComponent<FloatingText>();
            text.Show(message, color, size);
        }

        public static void DamageNumber(Vector3 position, int amount)
        {
            Vector3 spot = position + Vector3.up * 1.6f + Random.insideUnitSphere * 0.2f;
            FloatingLabel(spot, amount.ToString(), Color.white, 3.2f);
        }

        public static void Sparks(Vector3 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (LiveSparks >= MaxSparks)
                {
                    return;
                }

                GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = "Spark";
                Destroy(spark.GetComponent<Collider>());
                spark.transform.position = position;
                spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.15f);
                spark.GetComponent<Renderer>().sharedMaterial = Palette.UnlitMaterial(color);

                Spark mover = spark.AddComponent<Spark>();
                mover.velocity = Random.onUnitSphere * 6f + Vector3.up * 2f;
                LiveSparks++;
            }
        }
    }

    // One spark: flies out, shrinks, and deletes itself.
    public class Spark : MonoBehaviour
    {
        public Vector3 velocity;
        float life = 0.5f;

        void Update()
        {
            life -= Time.deltaTime;
            velocity += Physics.gravity * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.localScale *= 0.92f;
            if (life <= 0f)
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            Effects.LiveSparks--;
        }
    }
}
