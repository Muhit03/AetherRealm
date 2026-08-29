using UnityEngine;
using UnityEngine.AI;

namespace AetherRealm
{
    // Gives a character a short shove when it is hit. Works whether the
    // character moves with a CharacterController, a NavMeshAgent, or neither.
    public class Knockback : MonoBehaviour
    {
        CharacterController characterController;
        NavMeshAgent agent;
        Vector3 velocity;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            agent = GetComponent<NavMeshAgent>();
        }

        public void Push(Vector3 direction, float force)
        {
            direction.y = 0f;
            velocity += direction.normalized * force;
        }

        void Update()
        {
            if (velocity.magnitude < 0.05f)
            {
                velocity = Vector3.zero;
                return;
            }

            Vector3 step = velocity * Time.deltaTime;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(step);
                // don't let a shove push the agent off the walkable floor
                if (!agent.isOnNavMesh)
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(transform.position, out hit, 8f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                    velocity = Vector3.zero;
                }
            }
            else if (characterController != null && characterController.enabled)
            {
                characterController.Move(step);
            }
            else
            {
                transform.position += step;
            }

            // slow down (friction)
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, 9f * Time.deltaTime);
        }
    }
}
