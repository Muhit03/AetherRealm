using UnityEngine;

namespace AetherRealm
{
    // Small shared helpers for hit detection. The OverlapSphere here reuses one
    // array instead of making a new one every attack, which keeps the garbage
    // collector quiet during busy waves.
    public static class CombatUtil
    {
        static readonly Collider[] Buffer = new Collider[24];

        // Fills the shared buffer with colliders inside the sphere and returns
        // how many there are. Read results with GetHit(i).
        public static int OverlapSphere(Vector3 center, float radius)
        {
            return Physics.OverlapSphereNonAlloc(center, radius, Buffer);
        }

        public static Collider GetHit(int index)
        {
            return Buffer[index];
        }

        // Is there a clear line from the eye position to the target? Used by the
        // archers to decide whether they can actually shoot the player.
        public static bool CanSee(Vector3 eye, Transform target)
        {
            if (target == null)
            {
                return false;
            }

            Vector3 aim = target.position + Vector3.up * 0.8f;
            Vector3 dir = aim - eye;
            float dist = dir.magnitude;

            if (Physics.Raycast(eye, dir / dist, out RaycastHit hit, dist))
            {
                // we hit something before the player - the shot is blocked
                return hit.collider.GetComponentInParent<PlayerController>() != null;
            }
            return true;
        }
    }
}
