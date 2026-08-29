using UnityEngine;
using AetherRealm;

/// <summary>
/// A non-character object that still implements <see cref="IDamageable"/>. The
/// same weapon-overlap code that hits players and enemies smashes this barrel
/// with no special-casing — the payoff of polymorphism.
/// </summary>
public class DestructibleBarrel : MonoBehaviour, IDamageable
{
    [SerializeField] int health = 20;
    [SerializeField] int gold = 10;

    public void TakeDamage(int amount)
    {
        health -= Mathf.Abs(amount);
        transform.localScale *= 0.94f;
        if (health > 0) return;

        Effects.Sparks(transform.position + Vector3.up * 0.4f, Palette.Wood, 12);
        AudioManager.Play(AudioManager.Sound.EnemyDown);

        if (Random.value < 0.6f)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            var pc = player != null ? player.GetComponent<PlayerController>() : null;
            if (pc != null) PickupOrb.Spawn(transform.position + Vector3.up * 0.5f, pc, gold, 0);
        }
        Destroy(gameObject);
    }
}
