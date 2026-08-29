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
    public int manaShield = 3;      // damage soaked per hit while mana lasts
    public int boltDamage = 12;     // basic left-click bolt
    public float boltSpeed = 26f;
    public int blastCost = 25;      // the Q ability - a fan of bolts
    public int blastBoltDamage = 15;
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

    // Polymorphism: mana soaks a little of each hit before health is touched
    // (a chip, not a full block).
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

    // Polymorphism: the Mage's basic attack is a RANGED bolt, not a melee swing.
    public override void Attack()
    {
        if (attackTimer > 0f || isDodging)
        {
            return;
        }
        attackTimer = attackCooldown;

        if (Animator != null) Animator.PlayCast(0.25f);
        AudioManager.Play(AudioManager.Sound.Swing);

        FireBolt(AimDirection(), boltDamage);
        mana = Mathf.Min(maxMana, mana + 2); // basic bolts even give a little mana back
    }

    public override bool UseAbility()
    {
        if (mana < blastCost)
        {
            return false;
        }
        mana -= blastCost;

        if (Animator != null) Animator.PlayCast(0.6f);

        Vector3 forward = AimDirection();
        int bolts = Mathf.Max(1, boltCount);
        for (int i = 0; i < bolts; i++)
        {
            // spread the bolts evenly across a 45 degree fan
            float side = (bolts == 1) ? 0f : (i / (float)(bolts - 1)) - 0.5f;
            Vector3 direction = Quaternion.Euler(0f, side * 45f, 0f) * forward;
            FireBolt(direction, blastBoltDamage);
        }

        Effects.Shake(0.3f);
        return true;
    }

    // Direction from the staff toward where the mouse is pointing (kept level).
    Vector3 AimDirection()
    {
        Vector3 target = AimPoint;
        target.y = transform.position.y + 0.8f;
        Vector3 dir = target - (transform.position + Vector3.up * 0.9f);
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = transform.forward;
        }
        return dir.normalized;
    }

    void FireBolt(Vector3 direction, int damage)
    {
        Vector3 start = transform.position + Vector3.up * 0.9f + direction * 0.6f;
        Projectile.Spawn(start, direction, Projectile.Side.Player, damage, boltSpeed, Palette.MageGlow, gameObject);
    }
}
