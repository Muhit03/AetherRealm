using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AetherRealm;

/// <summary>
/// Login / register screen. Talks to <see cref="AuthManager"/> (which talks to
/// the SQL Server database) and, on success, tells the bootstrapper to begin the
/// run with the chosen class.
/// </summary>
public class LoginPanel : MonoBehaviour
{
    TMP_InputField _user, _pass;
    TMP_Text _status;
    string _class = "Warrior";
    Button _warriorBtn, _mageBtn;

    public void Build()
    {
        var root = (RectTransform)transform;
        UIFactory.Stretch(root);
        var bg = UIFactory.Box(root, "BG", new Color(0.04f, 0.05f, 0.08f, 1f));
        UIFactory.Stretch(bg.rectTransform);

        var card = UIFactory.Box(root, "Card", UIFactory.Panel);
        UIFactory.At(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 720f));

        var title = UIFactory.Label(card.transform, "ENTER AETHERREALM", 46, TextAlignmentOptions.Center);
        UIFactory.At(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(700f, 60f));
        title.color = UIFactory.Accent;

        _user = UIFactory.Input(card.transform, "Username", false, new Vector2(620f, 70f));
        UIFactory.At((RectTransform)_user.transform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(620f, 70f));

        _pass = UIFactory.Input(card.transform, "Password", true, new Vector2(620f, 70f));
        UIFactory.At((RectTransform)_pass.transform, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(620f, 70f));

        var classLabel = UIFactory.Label(card.transform, "Choose your class", 28, TextAlignmentOptions.Center);
        UIFactory.At(classLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(620f, 36f));

        _warriorBtn = UIFactory.Button(card.transform, "WARRIOR", () => SelectClass("Warrior"), new Vector2(300f, 70f));
        UIFactory.At((RectTransform)_warriorBtn.transform, new Vector2(0.5f, 1f), new Vector2(-160f, -420f), new Vector2(300f, 70f));

        _mageBtn = UIFactory.Button(card.transform, "MAGE", () => SelectClass("Mage"), new Vector2(300f, 70f));
        UIFactory.At((RectTransform)_mageBtn.transform, new Vector2(0.5f, 1f), new Vector2(160f, -420f), new Vector2(300f, 70f));

        var login = UIFactory.Button(card.transform, "LOGIN", OnLogin, new Vector2(300f, 74f));
        UIFactory.At((RectTransform)login.transform, new Vector2(0.5f, 1f), new Vector2(-160f, -520f), new Vector2(300f, 74f));

        var reg = UIFactory.Button(card.transform, "REGISTER", OnRegister, new Vector2(300f, 74f));
        UIFactory.At((RectTransform)reg.transform, new Vector2(0.5f, 1f), new Vector2(160f, -520f), new Vector2(300f, 74f));

        var back = UIFactory.Button(card.transform, "< Back", () => UIManager.Instance.ShowMainMenu(), new Vector2(200f, 56f));
        UIFactory.At((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(200f, 56f));

        _status = UIFactory.Label(card.transform, "", 26, TextAlignmentOptions.Center);
        UIFactory.At(_status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(680f, 60f));

        SelectClass("Warrior");
    }

    void SelectClass(string c)
    {
        _class = c;
        Tint(_warriorBtn, c == "Warrior");
        Tint(_mageBtn, c == "Mage");
    }

    static void Tint(Button b, bool selected)
    {
        if (b == null) return;
        var img = b.targetGraphic as Image;
        if (img != null) img.color = selected ? UIFactory.Accent : new Color(0.16f, 0.18f, 0.24f, 1f);
    }

    void OnLogin()
    {
        if (!Validate(out string u, out string p)) return;
        bool ok;
        try { ok = AuthManager.Instance.Login(u, p); }
        catch (System.Exception e) { Fail("Database error: " + e.Message); return; }

        if (ok) Begin("Welcome back, " + u + "!");
        else Fail("Invalid username or password.");
    }

    void OnRegister()
    {
        if (!Validate(out string u, out string p)) return;
        bool ok;
        try { ok = AuthManager.Instance.Register(u, p, _class); }
        catch (System.Exception e) { Fail("Database error: " + e.Message); return; }

        if (ok) Begin("Account created. Good luck, " + u + "!");
        else Fail("That username is taken.");
    }

    bool Validate(out string user, out string pass)
    {
        user = _user.text.Trim();
        pass = _pass.text;
        if (string.IsNullOrEmpty(user)) { Fail("Enter a username."); return false; }
        if (pass.Length < 4) { Fail("Password needs 4+ characters."); return false; }
        return true;
    }

    void Fail(string msg)
    {
        _status.text = msg;
        _status.color = new Color(1f, 0.5f, 0.5f);
    }

    void Begin(string msg)
    {
        if (DatabaseManager.OfflineMode)
        {
            msg += "  (offline - progress saved on this PC)";
        }
        _status.text = msg;
        _status.color = new Color(0.5f, 1f, 0.6f);
        UIManager.Instance.ShowHUD();
        GameBootstrap.Instance.BeginRun(AuthManager.CurrentClassType);
    }
}
