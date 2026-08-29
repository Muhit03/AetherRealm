using System;
using UnityEngine;
using UnityEngine.AI;
using AetherRealm;

/// <summary>
/// Base class for every enemy (goblin, archer, boss).
///
/// OOP notes for the course:
///  - Inheritance: MeleeGoblin, RangedArcher and BossOgre all extend this class
///    and reuse its movement, health and death code.
///  - Polymorphism: DoAttack and OnDeath are virtual, so each enemy fights and
///    rewards the player in its own way.
///  - Abstraction: it uses an IEnemyState state machine, so this class does not
///    need to know whether the enemy is idle, chasing or attacking.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    public enum AttackStyle { Melee, Ranged }

    protected int maxHealth = 40;
    protected float moveSpeed = 3.2f;
    protected float attackRange = 2f;
    protected float attackCooldown = 1.4f;
    protected int scoreValue = 10;
    protected int goldValue = 8;
    protected AttackStyle attackStyle = AttackStyle.Melee;

    // Raised whenever any enemy dies. WaveManager listens to this to know when
    // the wave is finished. (A static event is shared by every enemy.)
    public static event Action<EnemyController> Died;

    int currentHealth;
    float damageScale = 1f;
    float attackTimer;
    bool dead;

    protected NavMeshAgent agent;
    protected ProceduralAnimator animator;
    DamageFlash damageFlash;
    Knockback knockback;
    HealthBar healthBar;
    IEnemyState currentState;

    public Transform Player { get; private set; }
    public int CurrentHealth { get { return currentHealth; } }
    public float AttackRange { get { return attackRange; } }
    public AttackStyle Style { get { return attackStyle; } }
    public bool IsDead { get { return dead; } }

    public float DistanceToPlayer
    {
        get { return Player == null ? 999f : Vector3.Distance(transform.position, Player.position); }
    }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            Player = playerObject.transform;
        }
    }

    protected virtual void Start()
    {
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.8f;

        BuildBody();
        ChangeState(new SpawnState());
    }

    // WaveManager calls this to make later waves harder.
    public void Scale(float healthMultiplier, float damageMultiplier, float speedMultiplier)
    {
        maxHealth = Mathf.CeilToInt(maxHealth * healthMultiplier);
        currentHealth = maxHealth;
        damageScale = damageMultiplier;
        moveSpeed *= speedMultiplier;
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
    }

    protected virtual void BuildBody()
    {
        MeshRenderer capsule = GetComponent<MeshRenderer>();
        if (capsule != null)
        {
            capsule.enabled = false;
        }

        CharacterRig rig = CharacterBuilder.Build(transform, ClothColor(), SkinColor(), BodySize(), Weapon());

        animator = gameObject.AddComponent<ProceduralAnimator>();
        animator.Setup(rig);

        damageFlash = gameObject.AddComponent<DamageFlash>();
        damageFlash.Setup(rig.renderers, Color.white);

        knockback = gameObject.AddComponent<Knockback>();
        healthBar = HealthBar.Attach(transform, 2.2f);
    }

    // Appearance hooks - each enemy overrides the ones it needs.
    protected virtual Color ClothColor() { return Palette.GoblinCloth; }
    protected virtual Color SkinColor() { return Palette.GoblinSkin; }
    protected virtual float BodySize() { return 0.85f; }
    protected virtual WeaponType Weapon() { return WeaponType.Club; }

    protected virtual void Update()
    {
        if (dead)
        {
            return;
        }
        // stop fighting once the run is over
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
        if (currentState != null)
        {
            currentState.Tick(this);
        }
    }

    // ---- state machine ----
    public void ChangeState(IEnemyState newState)
    {
        if (currentState != null)
        {
            currentState.Exit(this);
        }
        currentState = newState;
        currentState.Enter(this);
    }

    public void MoveTowardsPlayer()
    {
        if (Player != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(Player.position);
        }
    }

    public void MoveAwayFromPlayer()
    {
        if (Player != null && agent.isOnNavMesh)
        {
            Vector3 away = transform.position + (transform.position - Player.position).normalized * 4f;
            agent.isStopped = false;
            agent.SetDestination(away);
        }
    }

    public void StopMoving()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    public void FacePlayer()
    {
        if (Player == null)
        {
            return;
        }
        Vector3 direction = Player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
        }
    }

    // ---- attacking ----
    // Called by AttackState. Returns true if the attack fired.
    public bool TryAttack()
    {
        if (attackTimer > 0f || Player == null)
        {
            return false;
        }

        IDamageable target = Player.GetComponent<IDamageable>();
        if (target == null)
        {
            return false;
        }

        attackTimer = attackCooldown;
        FacePlayer();
        DoAttack(target);
        return true;
    }

    // Each enemy type does its own attack here (polymorphism).
    protected virtual void DoAttack(IDamageable target) { }

    protected int ScaledDamage(int baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageScale));
    }

    public void PushBack(Vector3 direction, float force)
    {
        if (knockback != null)
        {
            knockback.Push(direction, force);
        }
    }

    // ---- IDamageable ----
    public void TakeDamage(int amount)
    {
        if (dead)
        {
            return;
        }

        currentHealth -= amount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (damageFlash != null) damageFlash.Flash();
        if (animator != null) animator.PlayHit(Vector3.zero);
        if (healthBar != null) healthBar.SetAmount((float)currentHealth / maxHealth);
        Effects.DamageNumber(transform.position, amount);
        AudioManager.Play(AudioManager.Sound.EnemyHurt);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    void Die()
    {
        dead = true;

        if (agent != null) agent.enabled = false;
        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null) bodyCollider.enabled = false;

        Effects.Sparks(transform.position + Vector3.up, ClothColor(), 12);
        AudioManager.Play(AudioManager.Sound.EnemyDown);

        OnDeath();

        if (Died != null)
        {
            Died(this);
        }

        if (animator != null)
        {
            animator.deathFinished = delegate { Destroy(gameObject); };
            animator.PlayDeath();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Reward for killing this enemy. Base version gives score and a gold orb.
    protected virtual void OnDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill(scoreValue);
        }
        DropGold(goldValue);
    }

    protected void DropGold(int amount)
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            PlayerController player = playerObject.GetComponent<PlayerController>();
            if (player != null)
            {
                PickupOrb.Spawn(transform.position + Vector3.up, player, amount, 0);
            }
        }
    }
}
