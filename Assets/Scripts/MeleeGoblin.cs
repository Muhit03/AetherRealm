using UnityEngine;

/// <summary>
/// Inherits generic movement and health from EnemyController and
/// adds its own melee attack. This is the "derived class" from
/// the inheritance example: reuses everything from the base
/// class, adds only what makes this enemy unique.
/// </summary>
public class MeleeGoblin : EnemyController
{
    [SerializeField] private int attackDamage = 10;

    public void Attack(IDamageable target)
    {
        target.TakeDamage(attackDamage);
    }
}
