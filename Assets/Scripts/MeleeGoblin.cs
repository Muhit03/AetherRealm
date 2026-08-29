using UnityEngine;
using AetherRealm;

/// <summary>
/// A melee goblin. Inherits movement, health and death from
/// <see cref="EnemyController"/> (inheritance) and only adds its own club swing
/// in the overridden DoAttack (polymorphism).
/// </summary>
public class MeleeGoblin : EnemyController
{
    public int clubDamage = 9;

    protected override void Awake()
    {
        maxHealth = 34;
        moveSpeed = 3.4f;
        attackRange = 1.9f;
        attackCooldown = 1.3f;
        scoreValue = 10;
        goldValue = 8;
        attackStyle = AttackStyle.Melee;
        base.Awake();
    }

    protected override Color ClothColor() { return Palette.GoblinCloth; }
    protected override Color SkinColor() { return Palette.GoblinSkin; }
    protected override float BodySize() { return 0.82f; }
    protected override WeaponType Weapon() { return WeaponType.Club; }

    protected override void DoAttack(IDamageable target)
    {
        if (animator != null) animator.PlayAttack(0.35f);
        PushBack(transform.forward, 4f); // small lunge forward
        target.TakeDamage(ScaledDamage(clubDamage));
    }
}
