using UnityEngine;
using AetherRealm;

/// <summary>
/// Mage class. Inherits from <see cref="PlayerController"/> (inheritance). It
/// spends mana to soak damage and to cast, and its ability fires a fan of
/// arcane bolts (polymorphism).
/// </summary>
public class Mage : PlayerController
{
    public int maxMana = 100;
    public int manaShield = 8;
    public int blastCost = 25;
    public int boltDamage = 16;
    public int boltCount = 5;

    int mana;

    public int CurrentMana { get { return mana; } }
    public int MaxMana { get { return maxMana; } }
    public float ManaFraction { get { return (float)mana / maxMana; } }

    public override string ClassName { get { return "Mage"; } }

    protected override Color ClothColor() { return Palette.MageCloth; }
    protected override WeaponType WeaponForClass() { return WeaponType.Staff; }

    protected override void Awake()
    {
        base.Awake();
        mana = maxMana;
    }

    protected override void Update()
    {
        base.Update();
        if (mana < maxMana)
        {
            mana += Mathf.CeilToInt(12f * Time.deltaTime);
            if (mana > maxMana)
            {
                mana = maxMana;
            }
        }
    }

    // Polymorphism: mana absorbs some damage before health is touched.
    public override void TakeDamage(int amount)
    {
        if (amount > 0 && mana > 0)
        {
            int absorbed = Mathf.Min(mana, Mathf.Min(manaShield, amount));
            mana -= absorbed;
            amount -= absorbed;
        }
        base.TakeDamage(amount);
    }

    public override bool UseAbility()
    {
        if (mana < blastCost)
        {
            return false;
        }
        mana -= blastCost;

        if (Animator != null) Animator.PlayCast(0.6f);

        Vector3 start = transform.position + Vector3.up + transform.forward * 0.6f;
        int bolts = Mathf.Max(1, boltCount);
        for (int i = 0; i < bolts; i++)
        {
            // spread the bolts evenly across a 45 degree fan
            float side = (bolts == 1) ? 0f : (i / (float)(bolts - 1)) - 0.5f;
            Quaternion spread = Quaternion.Euler(0f, side * 45f, 0f);
            Vector3 direction = spread * transform.forward;
            Projectile.Spawn(start, direction, Projectile.Side.Player, boltDamage, 22f, Palette.MageGlow, gameObject);
        }

        Effects.Shake(0.3f);
        return true;
    }
}
