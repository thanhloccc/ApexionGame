#if (UNITY_EDITOR || DEVELOPMENT_BUILD || APEXION_HFSM_DEBUG) && !DISABLE_APEXION_CHECKS

using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// HFSM - Debugging.md §5 — hosts <see cref="MachineOverlayPanel"/> on a <see cref="UIDocument"/>
    /// so it renders in a development build, not just the Editor.
    /// </summary>
    /// <remarks>
    /// Add this to any GameObject carrying a <see cref="UIDocument"/> with a <c>PanelSettings</c>
    /// assigned (the project already has one for runtime UI Toolkit — see
    /// <c>Rts.Game/Hud/Ui/RtsPanelSettings.asset</c>), or spawn it from code. The in-game console
    /// toggle (<c>IVisualCommand</c>) is deliberately not wired here — it lives in
    /// <c>ApexionGame.Core.Samples</c> so this runtime assembly never references
    /// <c>EncosyTower.Core.Extended</c> (DEC-011); until that lands, toggle visibility through
    /// <see cref="SetVisible"/>.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MachineOverlayBehaviour : MonoBehaviour
    {
        [SerializeField]
        private string _debugNameFilter = string.Empty;

        [SerializeField]
        private Vector2 _screenMargin = new(12f, 12f);

        private UIDocument _document;
        private MachineOverlayPanel _panel;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();

            _panel = new MachineOverlayPanel();
            _panel.SetFilter(_debugNameFilter);
            _panel.style.left = _screenMargin.x;
            _panel.style.top = _screenMargin.y;

            _document.rootVisualElement.Add(_panel);
        }

        private void OnDisable()
        {
            _panel?.RemoveFromHierarchy();
            _panel = null;
        }

        public void SetVisible(bool visible)
        {
            if (_panel != null)
            {
                _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void Next() => _panel?.Next();

        public void Previous() => _panel?.Previous();

        public void SetFilter(string debugNamePrefix)
        {
            _debugNameFilter = debugNamePrefix ?? string.Empty;
            _panel?.SetFilter(_debugNameFilter);
        }
    }
}

#endif
