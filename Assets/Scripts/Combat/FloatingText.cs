using TMPro;
using UnityEngine;

namespace AetherRealm
{
    // A short-lived piece of text in the world that floats up and fades out.
    // Used for damage numbers, "+10 gold", "WAVE 3" and so on.
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingText : MonoBehaviour
    {
        TextMeshPro text;
        float life;
        float maxLife = 1f;
        Vector3 velocity;
        Color color;

        public void Show(string message, Color textColor, float size)
        {
            text = GetComponent<TextMeshPro>();
            text.text = message;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = textColor;
            text.fontStyle = FontStyles.Bold;
            if (Fonts.Default != null)
            {
                text.font = Fonts.Default;
            }

            color = textColor;
            velocity = new Vector3(Random.Range(-0.5f, 0.5f), 3f, 0f);
            life = maxLife;
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += velocity * Time.deltaTime;
            velocity.y -= 4f * Time.deltaTime;

            // fade out
            color.a = life / maxLife;
            if (text != null)
            {
                text.color = color;
            }

            // face the camera
            Camera camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }
        }
    }
}
