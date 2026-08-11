using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// The handful of elements this HUD is built from.
    /// </summary>
    /// <remarks>
    /// The interface is built in C# with inline styles rather than from UXML/USS assets, and that is a
    /// deliberate trade. A UXML asset's main-object id is content-derived, so a hand-authored scene cannot
    /// reference one reliably — and the scene here is hand-authored precisely so it stays readable in a diff.
    /// </remarks>
    public static class RtsWidgets
    {
        public static VisualElement Row()
        {
            var element = new VisualElement();

            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = Align.Center;

            return element;
        }

        public static VisualElement Column()
        {
            var element = new VisualElement();

            element.style.flexDirection = FlexDirection.Column;

            return element;
        }

        /// <summary>A framed panel with an optional heading.</summary>
        public static VisualElement Box(string title = null)
        {
            var box = Column();

            box.style.backgroundColor = RtsHudTheme.PanelBg;
            box.style.marginBottom = 6f;

            Border(box, RtsHudTheme.Edge);
            Radius(box, 6f);
            Padding(box, 8f);

            if (string.IsNullOrEmpty(title) == false)
            {
                var heading = Label(title, 10, RtsHudTheme.InkMuted, bold: true);

                heading.style.letterSpacing = 2f;
                heading.style.marginBottom = 6f;

                box.Add(heading);
            }

            return box;
        }

        public static Label Label(string text, int size, Color color, bool bold = false)
        {
            var label = new Label(text);

            label.style.fontSize = size;
            label.style.color = color;
            label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.marginTop = 0f;
            label.style.marginBottom = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            label.pickingMode = PickingMode.Ignore;

            return label;
        }

        public static Button Button(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };

            button.style.fontSize = 11;
            button.style.color = RtsHudTheme.Ink;
            button.style.backgroundColor = RtsHudTheme.ButtonBg;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.marginLeft = 0f;
            button.style.marginRight = 4f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.minHeight = 22f;

            Border(button, RtsHudTheme.Edge);
            Radius(button, 4f);
            Padding(button, 4f);

            return button;
        }

        /// <summary>A two-line button: a name, and something that changes — a cost or a cooldown.</summary>
        public static Button StackedButton(string title, Action onClick, out Label subtitle)
        {
            var button = Button(string.Empty, onClick);

            button.style.flexDirection = FlexDirection.Column;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.style.minHeight = 38f;

            subtitle = Label(string.Empty, 9, RtsHudTheme.InkMuted);

            button.Add(Label(title, 11, RtsHudTheme.Ink, bold: true));
            button.Add(subtitle);

            return button;
        }

        /// <summary>A "name … value" line whose value is rewritten every refresh.</summary>
        public static VisualElement StatLine(string name, out Label value)
        {
            var row = Row();

            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 2f;
            row.Add(Label(name, 10, RtsHudTheme.InkMuted));

            value = Label("0", 10, RtsHudTheme.Ink, bold: true);
            row.Add(value);

            return row;
        }

        /// <summary>A track plus a fill, for health and progress.</summary>
        public static VisualElement Meter(out VisualElement fill, Color color, float height = 6f)
        {
            var track = new VisualElement();

            track.style.height = height;
            track.style.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            track.style.overflow = Overflow.Hidden;
            track.pickingMode = PickingMode.Ignore;

            Radius(track, 3f);

            fill = new VisualElement();
            fill.style.height = height;
            fill.style.width = Length.Percent(100f);
            fill.style.backgroundColor = color;
            fill.pickingMode = PickingMode.Ignore;

            track.Add(fill);

            return track;
        }

        /// <summary>Marks a button as selected, or dims it when its command would be refused.</summary>
        public static void SetAvailability(Button button, bool available)
            => button.style.opacity = available ? 1f : RtsHudTheme.DisabledOpacity;

        public static void SetHighlight(Button button, bool highlighted, Color color)
        {
            button.style.backgroundColor = highlighted ? color : RtsHudTheme.ButtonBg;
            button.style.color = highlighted ? Color.black : RtsHudTheme.Ink;
        }

        public static void Border(VisualElement element, Color color, float width = 1f)
        {
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        public static void Radius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        public static void Padding(VisualElement element, float padding)
        {
            element.style.paddingTop = padding;
            element.style.paddingRight = padding;
            element.style.paddingBottom = padding;
            element.style.paddingLeft = padding;
        }
    }
}
