// Interface for saveable game objects (Abstraction concept for OOP class)
public interface ISaveable
{
    void Save();
    void Load(int playerId);
}
