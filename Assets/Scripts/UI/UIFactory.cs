using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AetherRealm
{
    /// <summary>Small helpers for assembling the entire UI from code (no prefabs).</summary>
    public static class UIFactory
    {
        public static readonly Color Ink = new Color(0.93f, 0.94f, 0.98f);
        public static readonly Color Panel = new Color(0.07f, 0.08f, 0.11f, 0.94f);
        public static readonly Color Accent = new Color(1f, 0.78f, 0.30f);

        public static Canvas Screen(string name, int order)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Box(Transform parent, string name, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static RectTransform Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
            return rt;
        }

        public static RectTransform At(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, int size, TextAlignmentOptions align)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Ink;
            Fonts.Apply(t);
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Vector2 size)
        {
            var rt = Rect("Button", parent);
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.16f, 0.18f, 0.24f, 1f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.28f, 0.32f, 0.42f);
            colors.pressedColor = Accent;
            btn.colors = colors;
            if (onClick != null)
                btn.onClick.AddListener(() => { AudioManager.Play(AudioManager.Sound.UiClick); onClick(); });

            var t = Label(rt, label, 34, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            return btn;
        }

        /// <summary>A left-to-right fill bar. Returns the fill image; set its fillAmount.</summary>
        public static Image Bar(Transform parent, Color color, Vector2 size)
        {
            var back = Box(parent, "BarBack", new Color(0f, 0f, 0f, 0.6f));
            back.rectTransform.sizeDelta = size;

            var fillRt = Rect("BarFill", back.transform);
            Stretch(fillRt, 3f);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            return fill;
        }

        public static TMP_InputField Input(Transform parent, string placeholder, bool password, Vector2 size)
        {
            var rt = Rect("Input", parent);
            rt.sizeDelta = size;
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.05f, 1f);

            var field = rt.gameObject.AddComponent<TMP_InputField>();

            var viewport = Rect("TextArea", rt);
            Stretch(viewport, 12f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var placeholderLabel = Label(viewport, placeholder, 30, TextAlignmentOptions.Left);
            Stretch(placeholderLabel.rectTransform);
            placeholderLabel.color = new Color(1f, 1f, 1f, 0.35f);

            var textLabel = Label(viewport, "", 30, TextAlignmentOptions.Left);
            Stretch(textLabel.rectTransform);

            field.textViewport = viewport;
            field.textComponent = textLabel;
            field.placeholder = placeholderLabel;
            field.fontAsset = textLabel.font;
            field.pointSize = 30;
            field.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.caretWidth = 2;
            field.customCaretColor = true;
            field.caretColor = Ink;
            return field;
        }

        public static VerticalLayoutGroup Column(Transform parent, float spacing, RectOffset padding)
        {
            var rt = Rect("Column", parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperCenter;
            return v;
        }
    }
}
