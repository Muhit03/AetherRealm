using UnityEngine;

/// <summary>
/// Attach to a weapon's trigger collider. Damages anything it
/// touches that implements IDamageable — enemy, player, or
/// barrel — without ever checking which one it is. This is the
/// single-swing-damages-anything polymorphism example.
/// </summary>
public class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 15;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
        }
    }
}
