using UnityEngine;
using AetherRealm;

/// <summary>
/// The wave-8 boss. Inherits the whole enemy system (inheritance), uses the
/// melee approach/attack states like a grunt but never blocks or staggers,
/// hits with a big area slam in the overridden DoAttack, and triggers the win
/// screen in the overridden OnDeath (polymorphism).
/// </summary>
public class BossOgre : EnemyController
{
    public int slamDamage = 18;
    public float slamRadius = 4f;

    protected override void Awake()
    {
        maxHealth = 850;
        moveSpeed = 2.7f;
        attackRange = 3.4f;
        attackCooldown = 2f;
        scoreValue = 500;
        goldValue = 250;
        behaviour = Behaviour.Boss;
        blockChance = 0f;
        base.Awake();
    }

    protected override Color ClothColor() { return Palette.BossSkin; }
    protected override Color SkinColor() { return Palette.BossSkin; }
    protected override float BodySize() { return 1.8f; }
    protected override WeaponType Weapon() { return WeaponType.Club; }

    protected override void Start()
    {
        base.Start();

        // make the agent bigger to match the bigger body
        agent.radius = 1.1f;
        agent.height = 3.5f;

        AudioManager.Play(AudioManager.Sound.BossRoar);
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowBossBar("Ogre Warlord", maxHealth);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!IsDead && HUDController.Instance != null)
        {
            HUDController.Instance.SetBossHealth(CurrentHealth);
        }
    }

    protected override void DoAttack(IDamageable target)
    {
        if (animator != null) animator.PlayAttack(0.6f);

        int count = CombatUtil.OverlapSphere(transform.position, slamRadius);
        for (int i = 0; i < count; i++)
        {
            PlayerController player = CombatUtil.GetHit(i).GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(ScaledDamage(slamDamage));
            }
        }

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(ring.GetComponent<Collider>());
        ring.transform.position = transform.position + Vector3.up * 0.1f;
        ring.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        ring.GetComponent<Renderer>().sharedMaterial = Palette.GlowMaterial(Palette.Flame);
        ring.AddComponent<RingExpand>().Play(slamRadius * 2f, 0.4f);

        Effects.Shake(0.8f);
        Effects.FreezeFrame(0.06f);
        AudioManager.Play(AudioManager.Sound.Hit);
    }

    protected override void OnDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill(scoreValue);
            GameManager.Instance.OnBossDefeated();
        }
        if (HUDController.Instance != null)
        {
            HUDController.Instance.HideBossBar();
        }
        DropGold(goldValue);
        Effects.Shake(1.2f);
    }
}
