using UnityEngine;

/// <summary>
/// A non-character object that still implements IDamageable. This
/// is the payoff of polymorphism: the same weapon hitbox that
/// damages the player or an enemy also destroys this barrel,
/// with zero special-case code.
/// </summary>
public class DestructibleBarrel : MonoBehaviour, IDamageable
{
    [SerializeField] private int health = 20;

    public void TakeDamage(int amount)
    {
        health -= amount;

        if (health <= 0)
            Destroy(gameObject);
    }
}
