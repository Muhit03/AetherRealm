using UnityEngine;
using AetherRealm;

/// <summary>
/// A treasure chest. Implements <see cref="IInteractable"/> (abstraction) — the
/// player's interaction code just calls <c>Interact()</c> without knowing this
/// is a chest. Opens once and showers the player with gold.
/// </summary>
public class InteractableChest : MonoBehaviour, IInteractable
{
    [SerializeField] int gold = 60;
    bool _open;

    public void Interact()
    {
        if (_open) return;
        _open = true;

        var player = GameObject.FindGameObjectWithTag("Player");
        var pc = player != null ? player.GetComponent<PlayerController>() : null;
        if (pc != null)
        {
            for (int i = 0; i < 6; i++)
                PickupOrb.Spawn(transform.position + Vector3.up * 0.6f, pc, gold / 6, 0);
        }

        // pop the lid
        var lid = transform.Find("Chest_Lid");
        if (lid != null) lid.localRotation = Quaternion.Euler(-70f, 0f, 0f);

        Effects.Sparks(transform.position + Vector3.up, Palette.Gold, 20);
        AudioManager.Play(AudioManager.Sound.Coin);
    }
}
