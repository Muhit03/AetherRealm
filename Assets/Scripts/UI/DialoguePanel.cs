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

    void Awake() => Instance = this;

    public void Build()
    {
        var root = (RectTransform)transform;
        UIFactory.Stretch(root);

        var box = UIFactory.Box(root, "Box", UIFactory.Panel);
        UIFactory.At(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1300f, 240f));

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
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (_typing != null) { StopCoroutine(_typing); _body.text = _full; _typing = null; }
            else gameObject.SetActive(false);
        }
    }
}
