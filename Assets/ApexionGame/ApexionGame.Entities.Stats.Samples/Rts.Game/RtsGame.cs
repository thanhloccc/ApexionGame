using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// The only <c>MonoBehaviour</c> in the sample: it wires the pieces together and pumps them once a frame.
    /// </summary>
    /// <remarks>
    /// Everything the scene contains is a camera, this component and a <see cref="UIDocument"/>. The field, the
    /// units and the entire interface are built in code, which keeps the scene diffable and leaves nothing to
    /// configure by hand.
    /// <para>
    /// Note how little is here. The match is a plain C# object, the loop is a plain C# object, and the HUD panels
    /// take an interface — so this file has almost no logic, and everything that does have logic can be tested
    /// without a scene.
    /// </para>
    /// </remarks>
    [AddComponentMenu("ApexionGame/Stats Samples/RTS Battle")]
    public sealed class RtsGame : MonoBehaviour, IRtsHudHost
    {
        /// <summary>The side the player commands. The other one is the AI's.</summary>
        public const int PlayerTeam = 0;

        [SerializeField] private RtsMatchSettings _settings = RtsMatchSettings.Default;

        [Tooltip("Let the AI play the player's side too, so the sample runs unattended.")]
        [SerializeField] private bool _autoPlayPlayerTeam = false;

        [Tooltip("Only needed when this GameObject's UIDocument has no panel settings of its own.")]
        [SerializeField] private PanelSettings _panelSettings = null;

        private readonly RtsInput _input = new();

        private RtsMatch _match;
        private RtsPlayerController _player;
        private RtsMatchLoop _loop;
        private RtsUnitViewSet _views;
        private RtsFieldView _field;
        private RtsCameraRig _rig;
        private RtsHud _hud;

        // ---- IRtsHudHost -----------------------------------------------------------------------

        public RtsMatch Match => _match;

        public RtsPlayerController Player => _player;

        public RtsMatchLoop Loop => _loop;

        public bool AutoPlay => _match != null && _match.DirectorOf(PlayerTeam).Enabled;

        public void ToggleAutoPlay()
        {
            var director = _match.DirectorOf(PlayerTeam);

            director.Enabled = director.Enabled == false;
        }

        /// <remarks>
        /// A match is a plain object that owns native memory, so starting over is "dispose one, build another"
        /// rather than a reset method that has to remember every field.
        /// </remarks>
        public void Restart()
        {
            _match.Dispose();
            _match = NewMatch();

            _views.Clear();
            _player.ClearSelection();
            _loop.Reset();
            _rig.Frame();
            _hud.OnMatchReplaced();
        }

        // ---- lifetime --------------------------------------------------------------------------

        private void Start()
        {
            _settings = _settings.Normalized();

            _match = NewMatch();
            _player = new RtsPlayerController(PlayerTeam);
            _loop = new RtsMatchLoop(_settings.tickSeconds);

            _rig = new RtsCameraRig(ResolveCamera(), _settings.fieldHalfLength, _settings.laneHalfWidth);
            _field = new RtsFieldView(transform, _settings.fieldHalfLength, _settings.laneHalfWidth);
            _views = new RtsUnitViewSet(transform);

            _hud = new RtsHud(ResolveDocument(), this);

            if (_hud.IsBuilt == false)
            {
                Debug.LogWarning(
                    "[RTS sample] The HUD could not be built: the UIDocument on this GameObject has no "
                    + "PanelSettings, so UI Toolkit has no panel to draw into. Assign "
                    + "Rts.Game/Hud/Ui/RtsPanelSettings.asset to the UIDocument (or to this component's Panel "
                    + "Settings field). The battle itself runs regardless.", this);
            }
        }

        private RtsMatch NewMatch()
        {
            var match = new RtsMatch(_settings);

            match.DirectorOf(PlayerTeam).Enabled = _autoPlayPlayerTeam;

            return match;
        }

        private UIDocument ResolveDocument()
        {
            var document = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();

            if (document.panelSettings == null && _panelSettings != null)
            {
                document.panelSettings = _panelSettings;
            }

            return document;
        }

        private static Camera ResolveCamera()
        {
            var camera = Camera.main;

            if (camera != null)
            {
                return camera;
            }

            return new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        }

        private void OnDestroy()
        {
            _views?.Dispose();
            _field?.Dispose();
            _match?.Dispose();
        }

        // ---- frame -----------------------------------------------------------------------------

        private void Update()
        {
            if (_match == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;

            _input.Sample();

            var pointerOverUi = _hud.IsBuilt && _hud.IsPointerOver(_input.Pointer);

            if (_input.RestartPressed)
            {
                Restart();
                return;
            }

            _player.HandleKeys(_match, _input, _loop);
            _player.HandlePointer(_match, _input, _rig, pointerOverUi);
            _player.Tick(deltaTime);

            _loop.Advance(_match, deltaTime);

            _views.Consume(_match.Battle);
            _views.Sync(_match, _loop.Alpha, deltaTime, _player.Selected, _rig.Facing);

            _rig.Update(_input, deltaTime, pointerOverUi);

            SyncAimMarker(pointerOverUi);

            _hud.Refresh(deltaTime);
        }

        private void SyncAimMarker(bool pointerOverUi)
        {
            var aiming = _player.AimingSpell;

            if (aiming < 0 || pointerOverUi)
            {
                _field.HideAim();
                return;
            }

            var spell = RtsContent.Spells[aiming];
            var point = _rig.ScreenToGround(_input.Pointer);

            _field.ShowAim(new Vector2(point.x, point.y), spell.Radius, spell.IsDebuff);
        }
    }
}
