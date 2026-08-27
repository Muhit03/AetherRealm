using UnityEngine;

/// <summary>
/// Minimal test harness for DatabaseManager. Press P to create a
/// test player (first time only) and save their current position
/// and stats. Press O to load that player back and print the
/// result. Attach anywhere in the scene, e.g. on the Player.
/// </summary>
public class SaveLoadTester : MonoBehaviour
{
    private int testPlayerId = -1;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (testPlayerId == -1)
            {
                testPlayerId = DatabaseManager.Instance.CreatePlayer("TestHero");
                Debug.Log($"Created player with ID {testPlayerId}");
            }

            DatabaseManager.Instance.SavePlayerState(
                testPlayerId,
                level: 3,
                experience: 250,
                gold: 120,
                health: 80,
                maxHealth: 100,
                position: transform.position,
                districtId: 1);
        }

        if (Input.GetKeyDown(KeyCode.O) && testPlayerId != -1)
        {
            if (DatabaseManager.Instance.LoadPlayerState(testPlayerId, out var data))
            {
                Debug.Log($"Loaded {data.Username}: Level {data.Level}, Gold {data.Gold}, " +
                          $"Health {data.Health}/{data.MaxHealth}, Position {data.Position}");
            }
            else
            {
                Debug.Log("No saved data found for that player.");
            }
        }
    }
}
