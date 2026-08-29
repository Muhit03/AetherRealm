using UnityEngine;
using AetherRealm;

/// <summary>
/// Warrior class. Inherits everything from <see cref="PlayerController"/>
/// (inheritance). It reduces incoming damage in an overridden TakeDamage and
/// replaces the ability with a Shield Bash shockwave (polymorphism).
/// </summary>
public class Warrior : PlayerController
{
    public int armour = 6;
    public int bashDamage = 26;
    public float bashRadius = 3.4f;

    public override string ClassName { get { return "Warrior"; } }

    protected override Color ClothColor() { return Palette.WarriorCloth; }
    protected override WeaponType WeaponForClass() { return WeaponType.Sword; }

    // Polymorphism: a Warrior takes less damage than a normal player.
    public override void TakeDamage(int amount)
    {
        if (amount > 0)
        {
            amount -= armour;
            if (amount < 1)
            {
                amount = 1;
            }
        }
        base.TakeDamage(amount);
    }

    public override bool UseAbility()
    {
        if (Animator != null) Animator.PlayAttack(0.5f);

        // hit everything around us and push it away
        foreach (Collider hit in Physics.OverlapSphere(transform.position, bashRadius))
        {
            if (hit.transform.IsChildOf(transform))
            {
                continue;
            }

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null)
            {
                continue;
            }

            target.TakeDamage(bashDamage);

            Knockback targetKnockback = hit.GetComponentInParent<Knockback>();
            if (targetKnockback != null)
            {
                targetKnockback.Push(hit.transform.position - transform.position, 10f);
            }
        }

        SpawnShockwave();
        Effects.Shake(0.6f);
        Effects.FreezeFrame(0.06f);
        return true;
    }

    void SpawnShockwave()
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(ring.GetComponent<Collider>());
        ring.transform.position = transform.position + Vector3.up * 0.1f;
        ring.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
        ring.GetComponent<Renderer>().sharedMaterial = Palette.GlowMaterial(Palette.WarriorSteel);
        ring.AddComponent<RingExpand>().Play(bashRadius * 2f, 0.35f);
    }
}
