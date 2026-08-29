using System.Collections;
using TMPro;
using UnityEngine;
using AetherRealm;

/// <summary>A small typewriter dialogue box used by <see cref="NPCQuestGiver"/>.</summary>
public class DialoguePanel : MonoBehaviour
{
    public static DialoguePanel Instance { get; private set; }

    TMP_Text _speaker, _body;
    Coroutine _typing;
    string _full = "";
    float _autoCloseTimer;

    void Awake() => Instance = this;

    public void Build()
    {
        var root = (RectTransform)transform;
        UIFactory.Stretch(root);

        var box = UIFactory.Box(root, "Box", UIFactory.Panel);
        UIFactory.At(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1200f, 150f));

        _speaker = UIFactory.Label(box.transform, "", 34, TextAlignmentOptions.Left);
        UIFactory.At(_speaker.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -20f), new Vector2(600f, 44f));
        _speaker.color = UIFactory.Accent;

        _body = UIFactory.Label(box.transform, "", 30, TextAlignmentOptions.TopLeft);
        UIFactory.At(_body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(1220f, 150f));

        var hint = UIFactory.Label(box.transform, "[E] continue", 22, TextAlignmentOptions.Right);
        UIFactory.At(hint.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 20f), new Vector2(300f, 30f));

        gameObject.SetActive(false);
    }

    public void Say(string speaker, string text)
    {
        gameObject.SetActive(true);
        _speaker.text = speaker;
        _full = text;
        _autoCloseTimer = 3.5f + text.Length * 0.02f;   // closes itself after a while
        if (_typing != null) StopCoroutine(_typing);
        _typing = StartCoroutine(Type());
    }

    IEnumerator Type()
    {
        _body.text = "";
        for (int i = 0; i < _full.Length; i++)
        {
            _body.text += _full[i];
            if (i % 2 == 0) AudioManager.Play(AudioManager.Sound.UiClick);
            yield return new WaitForSeconds(0.02f);
        }
        _typing = null;
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        // close itself so it never blocks the screen during a fight
        _autoCloseTimer -= Time.deltaTime;
        if (_autoCloseTimer <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        // E or Space skips the typing / closes early (not left-click - that's attack)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            if (_typing != null) { StopCoroutine(_typing); _body.text = _full; _typing = null; }
            else gameObject.SetActive(false);
        }
    }
}
