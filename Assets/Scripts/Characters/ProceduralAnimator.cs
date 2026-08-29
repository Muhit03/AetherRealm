using UnityEngine;

namespace AetherRealm
{
    // Animates a CharacterRig with code instead of animation clips.
    //  - while the character moves, the arms and legs swing and the body bobs
    //  - PlayAttack swings the right arm forward
    //  - PlayHit makes the body flinch backwards
    //  - PlayDeath topples the character over and sinks it into the ground
    public class ProceduralAnimator : MonoBehaviour
    {
        CharacterRig rig;
        Vector3 lastPosition;
        float modelBaseHeight; // resting Y of the model, so the bob adds to it
        float walkCycle;       // keeps counting up; sin() of it gives the swing
        float currentSpeed;    // 0 = still, 1 = running

        float attackTimer;
        float attackLength;
        float castTimer;
        float hitTimer;

        bool dead;
        float deathProgress;
        public System.Action deathFinished;

        public bool IsDead { get { return dead; } }

        public void Setup(CharacterRig characterRig)
        {
            rig = characterRig;
            lastPosition = transform.position;
            modelBaseHeight = rig.model.localPosition.y;
        }

        public void PlaySpawn()
        {
            // nothing fancy - the character just appears
        }

        public void PlayAttack(float duration = 0.4f)
        {
            attackLength = duration;
            attackTimer = duration;
        }

        public void PlayCast(float duration = 0.5f)
        {
            castTimer = duration;
        }

        public void PlayHit(Vector3 fromDirection)
        {
            hitTimer = 0.2f;
        }

        public void PlayDeath()
        {
            dead = true;
        }

        void LateUpdate()
        {
            if (rig == null || rig.model == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            if (dead)
            {
                UpdateDeath(deltaTime);
                return;
            }

            MeasureSpeed(deltaTime);
            UpdateWalk(deltaTime);
            UpdateArms(deltaTime);
        }

        void MeasureSpeed(float deltaTime)
        {
            Vector3 movement = transform.position - lastPosition;
            lastPosition = transform.position;
            movement.y = 0f;

            float speed = movement.magnitude / deltaTime;
            float target = Mathf.Clamp01(speed / 5f);
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, deltaTime * 6f);
        }

        void UpdateWalk(float deltaTime)
        {
            walkCycle += deltaTime * (2f + currentSpeed * 9f);

            float swing = Mathf.Sin(walkCycle) * (5f + currentSpeed * 40f);
            SetAngle(rig.rightLeg, swing);
            SetAngle(rig.leftLeg, -swing);

            float bob = Mathf.Abs(Mathf.Sin(walkCycle)) * (0.03f + currentSpeed * 0.08f);
            rig.model.localPosition = new Vector3(0f, modelBaseHeight + bob, 0f);

            // lean forward a little when running
            rig.body.localRotation = Quaternion.Euler(currentSpeed * 12f, 0f, 0f);
        }

        void UpdateArms(float deltaTime)
        {
            if (attackTimer > 0f)
            {
                attackTimer -= deltaTime;
            }
            if (castTimer > 0f)
            {
                castTimer -= deltaTime;
            }
            if (hitTimer > 0f)
            {
                hitTimer -= deltaTime;
            }

            float armSwing = Mathf.Sin(walkCycle) * (5f + currentSpeed * 30f);
            SetAngle(rig.leftArm, -armSwing);

            float rightArmAngle = armSwing;

            if (attackTimer > 0f && attackLength > 0f)
            {
                // 0 at the start of the swing, 1 at the end
                float progress = 1f - (attackTimer / attackLength);
                rightArmAngle = Mathf.Lerp(40f, -150f, progress);
            }
            else if (castTimer > 0f)
            {
                rightArmAngle = -110f;
                SetAngle(rig.leftArm, -110f);
            }

            SetAngle(rig.rightArm, rightArmAngle);

            if (hitTimer > 0f)
            {
                rig.body.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            }
        }

        void SetAngle(Transform limb, float xAngle)
        {
            if (limb != null)
            {
                limb.localRotation = Quaternion.Euler(xAngle, 0f, 0f);
            }
        }

        void UpdateDeath(float deltaTime)
        {
            deathProgress += deltaTime;

            // fall over during the first second
            float fall = Mathf.Clamp01(deathProgress);
            rig.model.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(0f, 0f, 85f), fall);

            // then sink and shrink and disappear
            if (deathProgress > 1f)
            {
                rig.model.localPosition += Vector3.down * deltaTime * 0.6f;
                rig.model.localScale *= 0.96f;
            }

            if (deathProgress > 2f && deathFinished != null)
            {
                System.Action callback = deathFinished;
                deathFinished = null;
                callback();
            }
        }
    }
}
