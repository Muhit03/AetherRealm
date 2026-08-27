/// <summary>
/// One state in an enemy's AI state machine (Idle, Chase, Attack).
/// EnemyController just calls Enter/Tick/Exit without needing to
/// know what the enemy is currently doing — that complexity is
/// hidden behind this interface.
/// </summary>
public interface IEnemyState
{
    void Enter(EnemyController enemy);
    void Tick(EnemyController enemy);
    void Exit(EnemyController enemy);
}
