/// <summary>
/// Anything that can take damage implements this: the player,
/// enemies, and destructible objects like barrels. A single
/// weapon hit can call TakeDamage() on whatever it collides with
/// without ever checking what type of object it is.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
}
