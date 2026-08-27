using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base class for all enemies. Handles generic movement (via
/// NavMeshAgent), health, and the state machine. Derived classes
/// like MeleeGoblin inherit all of this and add their own unique
/// attack behavior on top.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 50;

    private int currentHealth;
    private IEnemyState currentState;

    public NavMeshAgent Agent { get; private set; }
    public Transform PlayerTarget { get; private set; }
    public int CurrentHealth => currentHealth;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            PlayerTarget = player.transform;
    }

    protected virtual void Start()
    {
        ChangeState(new IdleState());
    }

    protected virtual void Update()
    {
        currentState?.Tick(this);
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    public void MoveTo(Vector3 destination)
    {
        if (Agent != null && Agent.isOnNavMesh)
            Agent.SetDestination(destination);
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (currentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
