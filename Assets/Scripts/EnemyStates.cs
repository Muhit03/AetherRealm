using UnityEngine;

/// <summary>
/// Enemy stands still until it spots the player, then switches to Chase.
/// </summary>
public class IdleState : IEnemyState
{
    public void Enter(EnemyController enemy) { }

    public void Tick(EnemyController enemy)
    {
        if (enemy.PlayerTarget != null)
            enemy.ChangeState(new ChaseState());
    }

    public void Exit(EnemyController enemy) { }
}

/// <summary>
/// Enemy moves toward the player using NavMesh pathfinding until
/// close enough to attack.
/// </summary>
public class ChaseState : IEnemyState
{
    private const float attackDistance = 2.2f;

    public void Enter(EnemyController enemy) { }

    public void Tick(EnemyController enemy)
    {
        if (enemy.PlayerTarget == null) return;

        enemy.MoveTo(enemy.PlayerTarget.position);

        float distance = Vector3.Distance(enemy.transform.position, enemy.PlayerTarget.position);
        if (distance <= attackDistance)
            enemy.ChangeState(new AttackState());
    }

    public void Exit(EnemyController enemy) { }
}

/// <summary>
/// Enemy attacks on a cooldown while in range, and drops back to
/// Chase if the player moves away.
/// </summary>
public class AttackState : IEnemyState
{
    private const float cooldown = 1.5f;
    private const float breakOffDistance = 2.5f;
    private float timer;

    public void Enter(EnemyController enemy)
    {
        timer = 0f;
    }

    public void Tick(EnemyController enemy)
    {
        if (enemy.PlayerTarget == null) return;

        float distance = Vector3.Distance(enemy.transform.position, enemy.PlayerTarget.position);
        if (distance > breakOffDistance)
        {
            enemy.ChangeState(new ChaseState());
            return;
        }

        timer += Time.deltaTime;

        if (timer >= cooldown &&
            enemy is MeleeGoblin goblin &&
            enemy.PlayerTarget.TryGetComponent<IDamageable>(out var target))
        {
            goblin.Attack(target);
            timer = 0f;
        }
    }

    public void Exit(EnemyController enemy) { }
}
