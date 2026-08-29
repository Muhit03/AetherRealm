using UnityEngine;
using AetherRealm;

/// <summary>
/// A melee goblin. Inherits movement, health, blocking and death from
/// <see cref="EnemyController"/> (inheritance) and only adds its own club swing
/// in the overridden DoAttack (polymorphism). It surrounds the player with its
/// group and sometimes raises a guard to block hits.
/// </summary>
public class MeleeGoblin : EnemyController
{
    public int clubDamage = 8;

    protected override void Awake()
    {
        maxHealth = 32;
        moveSpeed = 3.6f;
        attackRange = 1.9f;
        attackCooldown = 1.2f;
        scoreValue = 10;
        goldValue = 8;
        behaviour = Behaviour.Grunt;
        blockChance = 0.3f;
        base.Awake();
    }

    protected override Color ClothColor() { return Palette.GoblinCloth; }
    protected override Color SkinColor() { return Palette.GoblinSkin; }
    protected override float BodySize() { return 0.82f; }
    protected override WeaponType Weapon() { return WeaponType.Club; }

    protected override void DoAttack(IDamageable target)
    {
        if (animator != null) animator.PlayAttack(0.3f);
        PushBack(transform.forward, 3f); // small lunge forward
        target.TakeDamage(ScaledDamage(clubDamage));
    }
}
