using UnityEngine;
using AetherRealm;

/// <summary>
/// The hero the player controls. This is the base class for <see cref="Warrior"/>
/// and <see cref="Mage"/>.
///
/// OOP notes for the course:
///  - Abstraction: it implements IDamageable and ISaveable, so other code can
///    hurt it or save it without knowing it is "the player".
///  - Encapsulation: the health, gold and dead flag are private; other scripts
///    read them through properties and change them through methods like AddGold.
///  - Polymorphism: TakeDamage and UseAbility are virtual, so Warrior and Mage
///    can change what they do.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IDamageable, ISaveable
{
    [Header("Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 6f;
    public int attackDamage = 18;
    public float attackRange = 2.4f;
    public float attackCooldown = 0.45f;
    public float abilityCooldown = 6f;

    // Upgrades bought in the shop. GameManager sets these.
    [HideInInspector] public int bonusHealth;
    [HideInInspector] public int bonusDamage;
    [HideInInspector] public float bonusSpeed;
    [HideInInspector] public float cooldownScale = 1f;
    [HideInInspector] public float lifesteal;

    // Private state
    int currentHealth;
    int gold;
    bool isDead;
    int playerId = -1;

    float maxStamina = 100f;
    float stamina;
    protected float attackTimer;
    protected bool isDodging;
    float abilityTimer;
    int comboStep;
    float comboTimer;

    float dodgeTimer;
    Vector3 dodgeDirection;
    float fallingSpeed;

    CharacterController characterController;
    Camera playerCamera;
    ProceduralAnimator animator;
    DamageFlash damageFlash;
    Knockback knockback;
    CharacterRig rig;
    protected Vector3 aimPoint;

    // Where on the ground the mouse is pointing - Mage aims its bolts here.
    protected Vector3 AimPoint { get { return aimPoint; } }

    // Properties other scripts read
    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth + bonusHealth; } }
    public int Gold { get { return gold; } }
    public bool IsDead { get { return isDead; } }
    public int PlayerId { get { return playerId; } }
    public float StaminaFraction { get { return stamina / maxStamina; } }
    public int TotalAttackDamage { get { return attackDamage + bonusDamage; } }

    // True for a short window right after a swing. The goblins watch this to
    // decide when to raise a block.
    public bool IsAttacking { get { return attackTimer > attackCooldown * 0.35f; } }

    public virtual float AbilityFraction
    {
        get { return 1f - (abilityTimer / (abilityCooldown * cooldownScale)); }
    }

    public virtual string ClassName { get { return "Adventurer"; } }

    protected ProceduralAnimator Animator { get { return animator; } }

    protected virtual void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentHealth = MaxHealth;
        stamina = maxStamina;
    }

    protected virtual void Start()
    {
        playerCamera = Camera.main;
        BuildBody();
        UpdateHud();
    }

    // Makes the visible character and the components that animate it.
    void BuildBody()
    {
        MeshRenderer capsule = GetComponent<MeshRenderer>();
        if (capsule != null)
        {
            capsule.enabled = false;
        }

        rig = CharacterBuilder.Build(transform, ClothColor(), Palette.Skin, 1f, WeaponForClass());

        animator = gameObject.AddComponent<ProceduralAnimator>();
        animator.Setup(rig);

        damageFlash = gameObject.AddComponent<DamageFlash>();
        damageFlash.Setup(rig.renderers, Color.white);

        knockback = gameObject.AddComponent<Knockback>();
    }

    protected virtual Color ClothColor() { return Palette.WarriorCloth; }
    protected virtual WeaponType WeaponForClass() { return WeaponType.Sword; }

    protected virtual void Update()
    {
        if (isDead)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        if (attackTimer > 0f) attackTimer -= deltaTime;
        if (abilityTimer > 0f) abilityTimer -= deltaTime;
        if (comboTimer > 0f) comboTimer -= deltaTime; else comboStep = 0;
        if (stamina < maxStamina) stamina += 20f * deltaTime;

        UpdateAimPoint();
        Move(deltaTime);
        HandleButtons();
    }

    // Works out where on the ground the mouse is pointing.
    void UpdateAimPoint()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        Plane ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        float distance;
        if (ground.Raycast(ray, out distance))
        {
            aimPoint = ray.GetPoint(distance);
        }
    }

    void Move(float deltaTime)
    {
        // Movement is relative to the camera so "W" always means "up the screen".
        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;
        if (playerCamera != null)
        {
            cameraForward = playerCamera.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();
            cameraRight = playerCamera.transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 wish = cameraRight * horizontal + cameraForward * vertical;
        if (wish.magnitude > 1f)
        {
            wish.Normalize();
        }

        float speed = moveSpeed + moveSpeed * bonusSpeed;
        Vector3 flatMovement;

        if (isDodging)
        {
            dodgeTimer -= deltaTime;
            flatMovement = dodgeDirection * speed * 2.2f;
            if (dodgeTimer <= 0f)
            {
                isDodging = false;
            }
        }
        else
        {
            flatMovement = wish * speed;
        }

        // gravity
        if (characterController.isGrounded && fallingSpeed < 0f)
        {
            fallingSpeed = -2f;
        }
        fallingSpeed += Physics.gravity.y * deltaTime;

        Vector3 movement = flatMovement + Vector3.up * fallingSpeed;
        characterController.Move(movement * deltaTime);

        // face the way we are moving, or the mouse while attacking
        Vector3 faceDirection = flatMovement;
        if (attackTimer > attackCooldown * 0.3f)
        {
            faceDirection = aimPoint - transform.position;
        }
        faceDirection.y = 0f;
        if (faceDirection.magnitude > 0.1f)
        {
            Quaternion look = Quaternion.LookRotation(faceDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 14f * deltaTime);
        }
    }

    void HandleButtons()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Dodge();
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            TryUseAbility();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
        // Tab (leaderboard) is handled by UIManager so it works even when dead.
    }

    // The Warrior uses this melee swing. The Mage overrides it with a ranged bolt.
    public virtual void Attack()
    {
        if (attackTimer > 0f || isDodging)
        {
            return;
        }
        attackTimer = attackCooldown;

        // simple 3-hit combo if you keep clicking
        if (comboTimer > 0f) comboStep = (comboStep + 1) % 3; else comboStep = 0;
        comboTimer = 0.7f;

        if (animator != null) animator.PlayAttack();
        AudioManager.Play(AudioManager.Sound.Swing);

        int damage = TotalAttackDamage + comboStep * 3;
        Vector3 center = transform.position + Vector3.up * 0.9f + transform.forward * 0.6f;
        bool hitAnything = false;

        int count = CombatUtil.OverlapSphere(center, attackRange);
        for (int i = 0; i < count; i++)
        {
            Collider hit = CombatUtil.GetHit(i);
            if (hit.transform.IsChildOf(transform))
            {
                continue;
            }

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || target is PlayerController)
            {
                continue;
            }

            // only hit things roughly in front of us
            Vector3 toTarget = hit.transform.position - transform.position;
            toTarget.y = 0f;
            if (Vector3.Angle(transform.forward, toTarget) > 70f)
            {
                continue;
            }

            target.TakeDamage(damage);
            RecordDamage(damage);
            hitAnything = true;

            Knockback targetKnockback = hit.GetComponentInParent<Knockback>();
            if (targetKnockback != null)
            {
                targetKnockback.Push(toTarget, 3f);
            }

            if (lifesteal > 0f)
            {
                Heal(Mathf.CeilToInt(damage * lifesteal));
            }
        }

        ShowSwing(center);

        if (hitAnything)
        {
            Effects.FreezeFrame(0.05f);
            Effects.Shake(0.15f);
            AudioManager.Play(AudioManager.Sound.Hit);
        }
    }

    void ShowSwing(Vector3 position)
    {
        // a small flat sweep in front of the player - just a hint of the swing
        GameObject arc = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(arc.GetComponent<Collider>());
        arc.transform.position = position + transform.forward * 0.4f;
        arc.transform.rotation = Quaternion.LookRotation(Vector3.up, transform.forward);
        arc.transform.localScale = new Vector3(attackRange * 1.3f, attackRange * 0.9f, 1f);
        arc.GetComponent<Renderer>().sharedMaterial = Palette.UnlitMaterial(new Color(1f, 1f, 1f, 0.12f));
        Destroy(arc, 0.08f);
    }

    void Dodge()
    {
        if (isDodging || stamina < 30f)
        {
            return;
        }
        stamina -= 30f;
        isDodging = true;
        dodgeTimer = 0.3f;

        Vector3 wish = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (wish.magnitude > 0.1f && playerCamera != null)
        {
            dodgeDirection = playerCamera.transform.TransformDirection(wish);
        }
        else
        {
            dodgeDirection = -transform.forward;
        }
        dodgeDirection.y = 0f;
        dodgeDirection.Normalize();

        AudioManager.Play(AudioManager.Sound.Dodge);
    }

    void TryUseAbility()
    {
        if (abilityTimer > 0f)
        {
            return;
        }
        if (UseAbility())
        {
            abilityTimer = abilityCooldown * cooldownScale;
            AudioManager.Play(AudioManager.Sound.Ability);
        }
    }

    // Warrior and Mage override this. Return true if the ability actually fired.
    public virtual bool UseAbility()
    {
        return false;
    }

    // Adds to this run's "damage dealt" total, used by the leaderboard.
    public void RecordDamage(int amount)
    {
        if (amount > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddDamageDealt(amount);
        }
    }

    void Interact()
    {
        int count = CombatUtil.OverlapSphere(transform.position + transform.forward, 1.8f);
        for (int i = 0; i < count; i++)
        {
            IInteractable interactable = CombatUtil.GetHit(i).GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }
        }
    }

    // ---- IDamageable ----
    public virtual void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }
        if (amount < 0)
        {
            Heal(-amount);
            return;
        }
        if (isDodging)
        {
            return; // invincible during a dodge roll
        }

        if (amount <= 0)
        {
            return; // fully blocked / absorbed - no feedback needed
        }

        currentHealth -= amount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (damageFlash != null) damageFlash.Flash();
        if (animator != null) animator.PlayHit(transform.position - aimPoint);
        Effects.Shake(0.25f);
        Effects.DamageNumber(transform.position, amount);
        AudioManager.Play(AudioManager.Sound.PlayerHurt);
        ScreenEffects.FlashHurt();
        UpdateHud();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }
        currentHealth += amount;
        if (currentHealth > MaxHealth)
        {
            currentHealth = MaxHealth;
        }
        Effects.FloatingLabel(transform.position + Vector3.up * 2f, "+" + amount, Palette.Health, 3f);
        UpdateHud();
    }

    void Die()
    {
        isDead = true;
        if (animator != null) animator.PlayDeath();
        AudioManager.Play(AudioManager.Sound.PlayerDown);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeath();
        }
    }

    // ---- gold ----
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        gold += amount;
        Effects.FloatingLabel(transform.position + Vector3.up * 2.2f, "+" + amount + " gold", Palette.Gold, 2.6f);
        AudioManager.Play(AudioManager.Sound.Coin);
        UpdateHud();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || gold < amount)
        {
            return false;
        }
        gold -= amount;
        UpdateHud();
        return true;
    }

    public void SetPlayerId(int id)
    {
        playerId = id;
    }

    // Used when a saved run is resumed: put the gold back and start the
    // checkpoint wave at full health, with no pop-up text.
    public void RestoreProgress(int savedGold)
    {
        gold = savedGold;
        currentHealth = MaxHealth;
        UpdateHud();
    }

    void UpdateHud()
    {
        if (HUDController.Instance == null)
        {
            return;
        }
        HUDController.Instance.SetHealth(currentHealth, MaxHealth);
        HUDController.Instance.SetGold(gold);
        HUDController.Instance.ShowPlayerInfo(this);
    }

    // ---- ISaveable (talks to SQL Server through DatabaseManager) ----
    public void Save()
    {
        if (playerId < 0 || DatabaseManager.Instance == null)
        {
            return;
        }
        try
        {
            DatabaseManager.Instance.SavePlayerState(playerId, 1, 0, gold, currentHealth, MaxHealth, transform.position, 1);
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("Could not save player: " + error.Message);
        }
    }

    public void Load(int id)
    {
        playerId = id;
        if (DatabaseManager.Instance == null)
        {
            return;
        }
        try
        {
            PlayerSaveData data;
            if (DatabaseManager.Instance.LoadPlayerState(playerId, out data))
            {
                currentHealth = data.Health;
                maxHealth = data.MaxHealth;
                gold = data.Gold;
                UpdateHud();
            }
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("Could not load player: " + error.Message);
        }
    }
}
