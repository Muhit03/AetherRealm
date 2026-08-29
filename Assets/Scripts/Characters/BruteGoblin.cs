using UnityEngine;
using AetherRealm;

/// <summary>
/// A brute: a big, slow, tanky goblin that shows up from wave 4. It blocks a lot
/// and hits hard with a shove that knocks the player back. Inherits everything
/// from <see cref="EnemyController"/> and just tweaks the stats and the attack.
/// </summary>
public class BruteGoblin : EnemyController
{
    public int smashDamage = 16;

    protected override void Awake()
    {
        maxHealth = 90;
        moveSpeed = 2.4f;
        attackRange = 2.3f;
        attackCooldown = 1.8f;
        scoreValue = 30;
        goldValue = 20;
        behaviour = Behaviour.Grunt;
        blockChance = 0.55f;   // guards far more often than a normal goblin
        base.Awake();
    }

    protected override Color ClothColor() { return new Color(0.30f, 0.42f, 0.22f); }
    protected override Color SkinColor() { return Palette.GoblinSkin; }
    protected override float BodySize() { return 1.15f; }
    protected override WeaponType Weapon() { return WeaponType.Club; }

    protected override void DoAttack(IDamageable target)
    {
        if (animator != null) animator.PlayAttack(0.45f);
        target.TakeDamage(ScaledDamage(smashDamage));

        // shove the player away
        if (Player != null)
        {
            Knockback playerKnockback = Player.GetComponent<Knockback>();
            if (playerKnockback != null)
            {
                playerKnockback.Push(Player.position - transform.position, 8f);
            }
        }
        Effects.Shake(0.3f);
    }
}
