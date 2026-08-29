using UnityEngine;

namespace AetherRealm
{
    // A travelling attack - an arrow or a magic bolt. It flies forward and
    // damages the first thing it touches that belongs to the other side. It
    // uses a short raycast each step instead of an overlap check, which is
    // cheaper and doesn't miss when the bolt is moving fast.
    public class Projectile : MonoBehaviour
    {
        public enum Side { Player, Enemy }

        Side side;
        int damage;
        float speed;
        float timeLeft;
        GameObject firedBy;

        public Side Team { get { return side; } }

        public static Projectile Spawn(Vector3 position, Vector3 direction, Side side, int damage, float speed, Color color, GameObject firedBy)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.3f;
            go.GetComponent<Renderer>().sharedMaterial = Palette.GlowMaterial(color);
            go.transform.forward = direction.normalized;

            Light light = go.AddComponent<Light>();
            light.color = color;
            light.range = 4f;
            light.intensity = 1.5f;

            Projectile projectile = go.AddComponent<Projectile>();
            projectile.side = side;
            projectile.damage = damage;
            projectile.speed = speed;
            projectile.timeLeft = 4f;
            projectile.firedBy = firedBy;
            return projectile;
        }

        void Update()
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step + 0.2f))
            {
                if (firedBy == null || !hit.collider.transform.IsChildOf(firedBy.transform))
                {
                    HitSomething(hit.collider, hit.point);
                    return;
                }
            }

            transform.position += transform.forward * step;
        }

        void HitSomething(Collider collider, Vector3 point)
        {
            PlayerController player = collider.GetComponentInParent<PlayerController>();
            EnemyController enemy = collider.GetComponentInParent<EnemyController>();

            bool hitsTarget = (side == Side.Player && enemy != null) || (side == Side.Enemy && player != null);

            if (hitsTarget)
            {
                IDamageable target = collider.GetComponentInParent<IDamageable>();
                target.TakeDamage(damage);

                if (side == Side.Player && player == null)
                {
                    // a bolt the player fired - count it towards damage dealt
                    GameObject me = GameObject.FindGameObjectWithTag("Player");
                    if (me != null)
                    {
                        PlayerController pc = me.GetComponent<PlayerController>();
                        if (pc != null) pc.RecordDamage(damage);
                    }
                }

                Effects.Sparks(point, Palette.Health, 5);
                Destroy(gameObject);
                return;
            }

            // hit an ally or a wall - just fizzle out
            if (player == null && enemy == null)
            {
                Effects.Sparks(point, Color.gray, 3);
                Destroy(gameObject);
            }
            else
            {
                // grazed an ally - skip past it and keep flying
                transform.position = point + transform.forward * 1.3f;
            }
        }
    }
}
