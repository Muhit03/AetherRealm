using UnityEngine;

/// <summary>
/// Legacy bootstrapper. The game now builds itself through
/// <see cref="GameBootstrap"/> (which is added automatically), so this component
/// only makes sure that entry point exists and then steps aside. Kept so the
/// old scene reference doesn't break.
/// </summary>
public class AutoWorldSetup : MonoBehaviour
{
    void Awake()
    {
        if (GameBootstrap.Instance == null && FindAnyObjectByType<GameBootstrap>() == null)
            gameObject.AddComponent<GameBootstrap>();
    }

    // No-op: retained for backwards compatibility with older scenes / menus.
    public void SetupWorld() { }
}
