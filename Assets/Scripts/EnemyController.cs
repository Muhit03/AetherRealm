using System;
using UnityEngine;
using UnityEngine.AI;
using AetherRealm;

/// <summary>
/// Base class for every enemy (goblin, brute, archer, boss).
///
/// OOP notes for the course:
///  - Inheritance: the enemy types all extend this class and reuse its
///    movement, health, blocking and death code.
///  - Polymorphism: DoAttack and OnDeath are virtual, so each enemy fights and
///    rewards the player in its own way.
///  - Abstraction: it uses an IEnemyState state machine, so this class never
///    checks "am I chasing, attacking or blocking" itself.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    public enum Behaviour { Grunt, Sniper, Boss }

    protected int maxHealth = 40;
    protected float moveSpeed = 3.2f;
    protected float attackRange = 2f;
    protected float attackCooldown = 1.4f;
    protected int scoreValue = 10;
    protected int goldValue = 8;
    protected Behaviour behaviour = Behaviour.Grunt;
    protected float blockChance = 0.35f;   // grunts only

    // Raised whenever any enemy dies. WaveManager listens so it knows when the
    // wave is finished. (One static event shared by every enemy.)
    public static event Action<EnemyController> Died;

    // Gives every grunt a different angle to stand at around the player, so a
    // group surrounds the player instead of forming a queue.
    static int slotCounter;
    float surroundAngle;

    int currentHealth;
    float damageScale = 1f;
    float attackTimer;
    bool dead;
    bool blocking;

    protected NavMeshAgent agent;
    protected ProceduralAnimator animator;
    DamageFlash damageFlash;
    Knockback knockback;
    HealthBar healthBar;
    IEnemyState currentState;

    public Transform Player { get; private set; }
    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public float AttackRange { get { return attackRange; } }
    public Behaviour Kind { get { return behaviour; } }
    public bool IsDead { get { return dead; } }
    public bool IsBlocking { get { return blocking; } }
    public bool CanAttackNow { get { return attackTimer <= 0f; } }
    public Vector3 EyePosition { get { return transform.position + Vector3.up * 1.4f; } }

    public float DistanceToPlayer
    {
        get { return Player == null ? 999f : Vector3.Distance(transform.position, Player.position); }
    }

    public bool CanSeePlayer
    {
        get { return CombatUtil.CanSee(EyePosition, Player); }
    }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        surroundAngle = (slotCounter++ * 55f) % 360f;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            Player = playerObject.transform;
        }
    }

    protected virtual void Start()
    {
        agent.speed = moveSpeed;
        agent.stoppingDistance = 0f;

        // The spawn portals sit near the wall - make sure we land on the NavMesh.
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 12f, NavMesh.AllAreas))
        {
            agent.Warp(navHit.position);
        }

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
            StopMoving();
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

    public string CurrentStateName
    {
        get { return currentState == null ? "none" : currentState.GetType().Name; }
    }

    // ---- movement helpers the states use ----
    public void SetDestination(Vector3 target)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target);
        }
    }

    public void MoveTowardsPlayer()
    {
        if (Player != null)
        {
            SetDestination(Player.position);
        }
    }

    // Path to a point on the player's ring, at this enemy's own angle, so a
    // group of grunts spreads out around the player.
    public void MoveToSurroundSpot()
    {
        if (Player == null)
        {
            return;
        }
        Vector3 offset = Quaternion.Euler(0f, surroundAngle, 0f) * Vector3.forward * (attackRange * 0.8f);
        SetDestination(Player.position + offset);
    }

    public void MoveAwayFromPlayer(float distance)
    {
        if (Player == null)
        {
            return;
        }
        Vector3 away = (transform.position - Player.position);
        away.y = 0f;
        SetDestination(transform.position + away.normalized * distance);
    }

    // Picks a spot behind one of the arena's cover walls, on the far side from
    // the player, that is a good distance for shooting. Used by the archers.
    public Vector3 FindCoverSpot()
    {
        ArenaLayout arena = GameBootstrap.Instance != null ? GameBootstrap.Instance.Arena : null;
        if (arena == null || arena.coverPoints.Count == 0 || Player == null)
        {
            return transform.position;
        }

        Vector3 best = transform.position;
        float bestCost = float.MaxValue;

        foreach (Vector3 cover in arena.coverPoints)
        {
            Vector3 fromPlayer = cover - Player.position;
            fromPlayer.y = 0f;
            Vector3 spot = cover + fromPlayer.normalized * 2.5f;

            float range = Vector3.Distance(spot, Player.position);
            if (range < 7f || range > 18f)
            {
                continue; // too close or too far to shoot well
            }

            float cost = Vector3.Distance(transform.position, spot);
            if (cost < bestCost)
            {
                bestCost = cost;
                best = spot;
            }
        }

        return best;
    }

    public void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
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
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 12f * Time.deltaTime);
        }
    }

    bool FacingPlayer()
    {
        if (Player == null)
        {
            return false;
        }
        Vector3 toPlayer = (Player.position - transform.position).normalized;
        return Vector3.Dot(transform.forward, toPlayer) > 0.25f;
    }

    // ---- blocking (grunts and brutes) ----
    public void StartBlock()
    {
        blocking = true;
        if (animator != null) animator.SetBlocking(true);
    }

    public void StopBlock()
    {
        blocking = false;
        if (animator != null) animator.SetBlocking(false);
    }

    // Should the enemy raise its guard right now? Yes if it just attacked and
    // rolls the block chance, or if the player is close and clearly attacking.
    public bool WantsToBlock()
    {
        if (Player == null || behaviour == Behaviour.Boss)
        {
            return false;
        }

        PlayerController pc = Player.GetComponent<PlayerController>();
        bool playerThreatening = pc != null && pc.IsAttacking && DistanceToPlayer < attackRange + 1.5f && FacingPlayer();

        return playerThreatening || UnityEngine.Random.value < blockChance;
    }

    // ---- attacking ----
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

        int taken = amount;

        // A raised guard soaks most of a hit that comes from the front.
        if (blocking && FacingPlayer())
        {
            taken = Mathf.Max(1, amount / 4);
            AudioManager.Play(AudioManager.Sound.Hit);
            Effects.Sparks(transform.position + transform.forward + Vector3.up, Color.white, 4);
            PushBack(-transform.forward, 2f);
        }

        currentHealth -= taken;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (damageFlash != null) damageFlash.Flash();
        if (healthBar != null) healthBar.SetAmount((float)currentHealth / maxHealth);
        Effects.DamageNumber(transform.position, taken);
        AudioManager.Play(AudioManager.Sound.EnemyHurt);

        if (currentHealth == 0)
        {
            Die();
            return;
        }

        // A big unblocked hit staggers the enemy for a moment.
        if (!blocking && taken >= maxHealth * 0.22f && behaviour != Behaviour.Boss)
        {
            if (animator != null) animator.PlayHit(Vector3.zero);
            ChangeState(new StaggerState());
        }
    }

    void Die()
    {
        dead = true;
        blocking = false;

        if (agent != null) agent.enabled = false;
        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null) bodyCollider.enabled = false;

        Effects.Sparks(transform.position + Vector3.up, ClothColor(), 8);
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
        if (Player != null)
        {
            PlayerController pc = Player.GetComponent<PlayerController>();
            if (pc != null)
            {
                PickupOrb.Spawn(transform.position + Vector3.up, pc, amount, 0);
            }
        }
    }
}
