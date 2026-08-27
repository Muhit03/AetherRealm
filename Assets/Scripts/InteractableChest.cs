using UnityEngine;

/// <summary>
/// A simple interactable object. The player's interaction code
/// only ever needs to call Interact() — it doesn't need to know
/// this is a chest specifically, which is the abstraction payoff
/// of IInteractable.
/// </summary>
public class InteractableChest : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isOpen = false;

    public void Interact()
    {
        if (isOpen) return;

        isOpen = true;
        Debug.Log("Chest opened! Loot dropped.");
    }
}
