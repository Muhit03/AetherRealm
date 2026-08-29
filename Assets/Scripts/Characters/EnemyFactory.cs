using UnityEngine;
using UnityEngine.AI;

namespace AetherRealm
{
    // Creates enemy GameObjects at runtime. Each enemy gets a NavMeshAgent (so
    // it can path around the arena) and a collider (so attacks can hit it). The
    // enemy's own script builds its body when it starts.
    public static class EnemyFactory
    {
        public enum Kind { Goblin, Archer, Ogre }

        public static EnemyController Create(Kind kind, Vector3 position)
        {
            GameObject enemyObject = new GameObject(kind.ToString());
            enemyObject.transform.position = position;

            CapsuleCollider collider = enemyObject.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0f, 0f, 0f);

            NavMeshAgent agent = enemyObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.baseOffset = 1f;
            agent.angularSpeed = 720f;
            agent.acceleration = 20f;

            if (kind == Kind.Goblin)
            {
                return enemyObject.AddComponent<MeleeGoblin>();
            }
            if (kind == Kind.Archer)
            {
                return enemyObject.AddComponent<RangedArcher>();
            }
            return enemyObject.AddComponent<BossOgre>();
        }
    }
}
