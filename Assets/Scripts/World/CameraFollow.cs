using UnityEngine;

namespace AetherRealm
{
    // A simple top-down chase camera. It follows the player smoothly and looks
    // down at a fixed angle. This is attached to the camera rig, not the camera.
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 15f, -11.5f);
        public float followSpeed = 5f;

        Vector3 moveVelocity;

        void LateUpdate()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
                return;
            }

            Vector3 wantedPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, wantedPosition, ref moveVelocity, 1f / followSpeed);
            transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        }
    }
}
