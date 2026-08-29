using UnityEngine;
using AetherRealm;

/// <summary>
/// Elder Eldrin. Implements <see cref="IInteractable"/> (abstraction): the
/// player's interaction code just calls <c>Interact()</c> and never needs to
/// know this is an NPC. Talking to him heals a little and opens the shop during
/// the intermission.
/// </summary>
public class NPCQuestGiver : MonoBehaviour, IInteractable
{
    public string npcName = "Elder Eldrin";
    int _lines;

    static readonly string[] Chatter =
    {
        "Steel yourself. The next wave gathers beyond the portals.",
        "Gold spent on my wares is gold well spent, hero.",
        "The Ogre Warlord leads them. End him and the sky heals.",
        "You fight well. The realm remembers its champions.",
    };

    public void Interact()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var pc = player != null ? player.GetComponent<PlayerController>() : null;

        bool intermission = ShopPanel.Instance != null && ShopPanel.Instance.isActiveAndEnabled;
        if (intermission)
        {
            DialoguePanel.Instance?.Say(npcName, "Look over my wares, then. Choose wisely.");
            return;
        }

        if (pc != null && !pc.IsDead) pc.Heal(15);
        DialoguePanel.Instance?.Say(npcName, Chatter[_lines++ % Chatter.Length] + "\n<size=22>(+15 health)</size>");
        AudioManager.Play(AudioManager.Sound.Buy);
    }
}
