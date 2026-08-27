/// <summary>
/// Anything the player can interact with (chests, doors, NPCs)
/// implements this. Keeps interaction logic behind one clean
/// method so the player's interaction code never needs to know
/// what kind of object it's dealing with.
/// </summary>
public interface IInteractable
{
    void Interact();
}
