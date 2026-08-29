using UnityEngine;
using UnityEngine.AI;

namespace AetherRealm
{
    // Creates enemy GameObjects at runtime. Each enemy gets a NavMeshAgent (so
    // it can path around the arena) and a collider (so attacks can hit it). The
    // enemy's own script builds its body when it starts.
    public static class EnemyFactory
    {
        public enum Kind { Goblin, Archer, Brute, Ogre }

        public static EnemyController Create(Kind kind, Vector3 position)
        {
            GameObject enemyObject = new GameObject(kind.ToString());
            enemyObject.transform.position = position;

            CapsuleCollider collider = enemyObject.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = Vector3.zero;

            NavMeshAgent agent = enemyObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.baseOffset = 1f;
            agent.angularSpeed = 720f;
            agent.acceleration = 24f;

            switch (kind)
            {
                case Kind.Goblin: return enemyObject.AddComponent<MeleeGoblin>();
                case Kind.Archer: return enemyObject.AddComponent<RangedArcher>();
                case Kind.Brute:  return enemyObject.AddComponent<BruteGoblin>();
                default:          return enemyObject.AddComponent<BossOgre>();
            }
        }
    }
}
