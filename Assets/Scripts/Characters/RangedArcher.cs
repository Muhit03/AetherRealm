using UnityEngine;
using AetherRealm;

/// <summary>
/// A ranged archer. Inherits from <see cref="EnemyController"/> (inheritance).
/// Its <c>Sniper</c> behaviour makes it hide behind cover, only shoot when it
/// has a clear line to the player, and flee when the player closes in. The
/// arrow itself is fired in the overridden DoAttack (polymorphism).
/// </summary>
public class RangedArcher : EnemyController
{
    public int arrowDamage = 7;
    public float arrowSpeed = 20f;

    protected override void Awake()
    {
        maxHealth = 22;
        moveSpeed = 3.4f;
        attackRange = 14f;
        attackCooldown = 1.8f;
        scoreValue = 20;
        goldValue = 14;
        behaviour = Behaviour.Sniper;
        base.Awake();
    }

    protected override Color ClothColor() { return Palette.ArcherCloth; }
    protected override Color SkinColor() { return Palette.Skin; }
    protected override float BodySize() { return 0.95f; }
    protected override WeaponType Weapon() { return WeaponType.Bow; }

    protected override void DoAttack(IDamageable target)
    {
        if (animator != null) animator.PlayCast(0.35f);
        if (Player == null)
        {
            return;
        }

        Vector3 start = EyePosition + transform.forward * 0.5f;
        Vector3 direction = (Player.position + Vector3.up * 0.8f - start).normalized;
        Projectile.Spawn(start, direction, Projectile.Side.Enemy, ScaledDamage(arrowDamage), arrowSpeed,
            new Color(0.9f, 0.8f, 0.5f), gameObject);
        AudioManager.Play(AudioManager.Sound.Swing);
    }
}
