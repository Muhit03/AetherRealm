using UnityEngine;

/// <summary>
/// Handles player movement and implements IDamageable. Health and
/// gold are private — the encapsulation example from the OOP
/// overview. Nothing outside this class can set them directly;
/// they can only change through validated methods like
/// TakeDamage() and AddGold(), which stops values from going
/// negative or getting corrupted by outside code.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float moveSpeed = 5f;

    private int currentHealth;
    private int gold;
    private bool isDead;
    private CharacterController controller;

    public int CurrentHealth => currentHealth;
    public int Gold => gold;
    public bool IsDead => isDead;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, 0f, v) * moveSpeed * Time.deltaTime;
        controller.Move(move);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (currentHealth <= 0)
        {
            isDead = true;
            Debug.Log("Player has died.");
        }
    }

    public void AddGold(int amount)
    {
        if (amount < 0) return;
        gold += amount;
    }
}