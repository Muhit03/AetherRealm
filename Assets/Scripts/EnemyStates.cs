using UnityEngine;

// The states an enemy can be in. EnemyController just calls Enter / Tick / Exit
// on whichever one is active - it never checks "am I chasing or attacking".
//
//  Grunts / Brutes:  Spawn -> MeleeApproach -> MeleeAttack <-> Block
//  Archers:          Spawn -> ArcherReposition -> ArcherShoot -> ArcherFlee
//  Boss:             Spawn -> MeleeApproach -> MeleeAttack
//  Anyone hit hard:  -> Stagger -> (back to approach)

// Just appeared at the portal. Stand still for a moment, then pick a plan
// based on what kind of enemy this is.
public class SpawnState : IEnemyState
{
    float timer;

    public void Enter(EnemyController enemy)
    {
        timer = 0.6f;
        enemy.StopMoving();
    }

    public void Tick(EnemyController enemy)
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }

        if (enemy.Kind == EnemyController.Behaviour.Sniper)
        {
            enemy.ChangeState(new ArcherRepositionState());
        }
        else
        {
            enemy.ChangeState(new MeleeApproachState());
        }
    }

    public void Exit(EnemyController enemy) { }
}

// Got hit hard - freeze for a moment.
public class StaggerState : IEnemyState
{
    float timer;

    public void Enter(EnemyController enemy)
    {
        timer = 0.4f;
        enemy.StopMoving();
        enemy.StopBlock();
    }

    public void Tick(EnemyController enemy)
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (enemy.Kind == EnemyController.Behaviour.Sniper)
            {
                enemy.ChangeState(new ArcherRepositionState());
            }
            else
            {
                enemy.ChangeState(new MeleeApproachState());
            }
        }
    }

    public void Exit(EnemyController enemy) { }
}

// ---------- melee ----------

// Close in on the player. Each enemy heads for its own spot on the ring around
// the player, so a group surrounds instead of forming a line.
public class MeleeApproachState : IEnemyState
{
    public void Enter(EnemyController enemy) { }

    public void Tick(EnemyController enemy)
    {
        enemy.MoveToSurroundSpot();

        if (enemy.DistanceToPlayer <= enemy.AttackRange)
        {
            enemy.ChangeState(new MeleeAttackState());
        }
    }

    public void Exit(EnemyController enemy) { }
}

// In range: face the player and swing. After each swing, maybe raise a guard.
public class MeleeAttackState : IEnemyState
{
    public void Enter(EnemyController enemy)
    {
        enemy.StopMoving();
    }

    public void Tick(EnemyController enemy)
    {
        if (enemy.DistanceToPlayer > enemy.AttackRange + 1.2f)
        {
            enemy.ChangeState(new MeleeApproachState());
            return;
        }

        enemy.FacePlayer();

        if (enemy.CanAttackNow)
        {
            enemy.TryAttack();

            if (enemy.WantsToBlock())
            {
                enemy.ChangeState(new BlockState());
            }
        }
    }

    public void Exit(EnemyController enemy) { }
}

// Hold a guard up while facing the player. Soaks hits from the front.
public class BlockState : IEnemyState
{
    float timer;

    public void Enter(EnemyController enemy)
    {
        timer = Random.Range(0.7f, 1.4f);
        enemy.StopMoving();
        enemy.StartBlock();
    }

    public void Tick(EnemyController enemy)
    {
        enemy.FacePlayer();
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            enemy.StopBlock();
            if (enemy.DistanceToPlayer <= enemy.AttackRange)
            {
                enemy.ChangeState(new MeleeAttackState());
            }
            else
            {
                enemy.ChangeState(new MeleeApproachState());
            }
        }
    }

    public void Exit(EnemyController enemy)
    {
        enemy.StopBlock();
    }
}

// ---------- archer ----------

// Move to a spot behind a cover wall with a clear shot at the player.
public class ArcherRepositionState : IEnemyState
{
    Vector3 target;
    float giveUp;

    public void Enter(EnemyController enemy)
    {
        target = enemy.FindCoverSpot();
        enemy.SetDestination(target);
        giveUp = 4f;
    }

    public void Tick(EnemyController enemy)
    {
        giveUp -= Time.deltaTime;

        bool arrived = Vector3.Distance(enemy.transform.position, target) < 1.5f;
        if (arrived || giveUp <= 0f)
        {
            enemy.ChangeState(new ArcherShootState());
        }
    }

    public void Exit(EnemyController enemy) { }
}

// Peek out and shoot while there is a clear line to the player.
public class ArcherShootState : IEnemyState
{
    float noSightTimer;
    float strafeTimer;

    public void Enter(EnemyController enemy)
    {
        enemy.StopMoving();
        noSightTimer = 0f;
    }

    public void Tick(EnemyController enemy)
    {
        float distance = enemy.DistanceToPlayer;

        // player got too close - run
        if (distance < 5f)
        {
            enemy.ChangeState(new ArcherFleeState());
            return;
        }

        // player ran out of range - find a new spot
        if (distance > enemy.AttackRange)
        {
            enemy.ChangeState(new ArcherRepositionState());
            return;
        }

        enemy.FacePlayer();

        if (enemy.CanSeePlayer)
        {
            noSightTimer = 0f;
            if (enemy.CanAttackNow)
            {
                enemy.TryAttack();
            }
        }
        else
        {
            // shot is blocked - shuffle sideways, and reposition if it stays blocked
            noSightTimer += Time.deltaTime;
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                strafeTimer = 0.6f;
                Vector3 side = Vector3.Cross(Vector3.up, enemy.transform.forward);
                enemy.SetDestination(enemy.transform.position + side * Random.Range(-3f, 3f));
            }
            if (noSightTimer > 1.2f)
            {
                enemy.ChangeState(new ArcherRepositionState());
            }
        }
    }

    public void Exit(EnemyController enemy) { }
}

// Sprint away from the player until there is room to shoot again.
public class ArcherFleeState : IEnemyState
{
    public void Enter(EnemyController enemy)
    {
        enemy.MoveAwayFromPlayer(10f);
    }

    public void Tick(EnemyController enemy)
    {
        if (enemy.DistanceToPlayer > 11f)
        {
            enemy.ChangeState(new ArcherRepositionState());
        }
        else
        {
            enemy.MoveAwayFromPlayer(10f);
        }
    }

    public void Exit(EnemyController enemy) { }
}
