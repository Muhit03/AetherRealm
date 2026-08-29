using UnityEngine;

// The three states an enemy can be in. EnemyController just calls
// Enter / Tick / Exit on whichever one is active - it never has to check
// "am I chasing or attacking" itself. The same states work for melee and
// ranged enemies because the attack is chosen by the enemy, not the state.

// Just spawned: stand still for a moment, then start chasing.
public class SpawnState : IEnemyState
{
    float timer;

    public void Enter(EnemyController enemy)
    {
        timer = 0.8f;
        enemy.StopMoving();
    }

    public void Tick(EnemyController enemy)
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            enemy.ChangeState(new ChaseState());
        }
    }

    public void Exit(EnemyController enemy) { }
}

// Move towards the player (melee) or get into shooting range (ranged).
public class ChaseState : IEnemyState
{
    public void Enter(EnemyController enemy) { }

    public void Tick(EnemyController enemy)
    {
        float distance = enemy.DistanceToPlayer;

        if (enemy.Style == EnemyController.AttackStyle.Ranged)
        {
            if (distance > enemy.AttackRange)
            {
                enemy.MoveTowardsPlayer();
            }
            else if (distance < enemy.AttackRange * 0.5f)
            {
                enemy.MoveAwayFromPlayer();
            }
            else
            {
                enemy.StopMoving();
                enemy.FacePlayer();
            }
        }
        else
        {
            enemy.MoveTowardsPlayer();
        }

        if (distance <= enemy.AttackRange)
        {
            enemy.ChangeState(new AttackState());
        }
    }

    public void Exit(EnemyController enemy) { }
}

// In range: face the player and attack on a cooldown.
public class AttackState : IEnemyState
{
    public void Enter(EnemyController enemy)
    {
        enemy.StopMoving();
    }

    public void Tick(EnemyController enemy)
    {
        float distance = enemy.DistanceToPlayer;

        // player ran away - go back to chasing
        if (distance > enemy.AttackRange + 1f)
        {
            enemy.ChangeState(new ChaseState());
            return;
        }

        enemy.FacePlayer();
        enemy.TryAttack();
    }

    public void Exit(EnemyController enemy) { }
}
