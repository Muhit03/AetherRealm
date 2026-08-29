using UnityEngine;

namespace AetherRealm
{
    // A travelling attack - an arrow or a magic bolt. It flies forward and
    // damages the first thing it touches that belongs to the other side.
    public class Projectile : MonoBehaviour
    {
        public enum Side { Player, Enemy }

        Side side;
        int damage;
        float speed;
        float timeLeft;
        GameObject firedBy;

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

            transform.position += transform.forward * speed * Time.deltaTime;

            Collider[] nearby = Physics.OverlapSphere(transform.position, 0.4f);
            foreach (Collider collider in nearby)
            {
                if (firedBy != null && collider.transform.IsChildOf(firedBy.transform))
                {
                    continue;
                }

                bool hitPlayer = collider.GetComponentInParent<PlayerController>() != null;
                bool hitEnemy = collider.GetComponentInParent<EnemyController>() != null;

                // does this projectile care about what it just touched?
                bool validTarget = (side == Side.Player && hitEnemy) || (side == Side.Enemy && hitPlayer);

                if (validTarget)
                {
                    IDamageable target = collider.GetComponentInParent<IDamageable>();
                    target.TakeDamage(damage);
                    Effects.Sparks(transform.position, Palette.Health, 6);
                    Destroy(gameObject);
                    return;
                }

                // hit a wall or pillar - just disappear
                bool hitScenery = !hitPlayer && !hitEnemy && collider.GetComponent<Renderer>() != null;
                if (hitScenery)
                {
                    Effects.Sparks(transform.position, Color.gray, 4);
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
