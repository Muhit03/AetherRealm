using UnityEngine;

namespace AetherRealm
{
    // A glowing orb dropped by enemies and chests. It waits a moment, then flies
    // to the player and gives them gold or health.
    public class PickupOrb : MonoBehaviour
    {
        PlayerController player;
        int gold;
        int health;
        float delay = 0.35f;
        float speed;
        Vector3 velocity;

        public static void Spawn(Vector3 position, PlayerController player, int gold, int health)
        {
            if (player == null)
            {
                return;
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PickupOrb";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position + Vector3.up * 0.4f;
            go.transform.localScale = Vector3.one * 0.3f;

            Color color = health > 0 ? Palette.Health : Palette.Gold;
            go.GetComponent<Renderer>().sharedMaterial = Palette.GlowMaterial(color);

            Light light = go.AddComponent<Light>();
            light.color = color;
            light.range = 3f;
            light.intensity = 1.2f;

            PickupOrb orb = go.AddComponent<PickupOrb>();
            orb.player = player;
            orb.gold = gold;
            orb.health = health;
            orb.velocity = new Vector3(Random.Range(-2f, 2f), 4f, Random.Range(-2f, 2f));
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            transform.Rotate(Vector3.up, 180f * deltaTime, Space.World);

            if (delay > 0f)
            {
                // pop up and fall back down before homing in
                delay -= deltaTime;
                velocity += Physics.gravity * deltaTime;
                transform.position += velocity * deltaTime;
                return;
            }

            if (player == null || player.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            speed = Mathf.MoveTowards(speed, 20f, 40f * deltaTime);
            Vector3 toPlayer = player.transform.position + Vector3.up - transform.position;
            transform.position += toPlayer.normalized * speed * deltaTime;

            if (toPlayer.magnitude < 0.8f)
            {
                if (gold > 0)
                {
                    player.AddGold(gold);
                }
                if (health > 0)
                {
                    player.Heal(health);
                }
                Destroy(gameObject);
            }
        }
    }
}
