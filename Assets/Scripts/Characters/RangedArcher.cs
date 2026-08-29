using UnityEngine;
using AetherRealm;

/// <summary>
/// A ranged archer. Inherits from <see cref="EnemyController"/> (inheritance),
/// keeps its distance because of the Ranged attack style, and fires an arrow
/// projectile in its overridden DoAttack (polymorphism).
/// </summary>
public class RangedArcher : EnemyController
{
    public int arrowDamage = 7;
    public float arrowSpeed = 18f;

    protected override void Awake()
    {
        maxHealth = 24;
        moveSpeed = 3f;
        attackRange = 11f;
        attackCooldown = 2f;
        scoreValue = 20;
        goldValue = 14;
        attackStyle = AttackStyle.Ranged;
        base.Awake();
    }

    protected override Color ClothColor() { return Palette.ArcherCloth; }
    protected override Color SkinColor() { return Palette.Skin; }
    protected override float BodySize() { return 0.95f; }
    protected override WeaponType Weapon() { return WeaponType.Bow; }

    protected override void DoAttack(IDamageable target)
    {
        if (animator != null) animator.PlayCast(0.4f);
        if (Player == null)
        {
            return;
        }

        Vector3 start = transform.position + Vector3.up + transform.forward * 0.5f;
        Vector3 direction = (Player.position + Vector3.up * 0.8f - start).normalized;
        Projectile.Spawn(start, direction, Projectile.Side.Enemy, ScaledDamage(arrowDamage), arrowSpeed,
            new Color(0.9f, 0.8f, 0.5f), gameObject);
    }
}
