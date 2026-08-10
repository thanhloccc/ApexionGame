using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// One unit on screen: a body, a health bar, two effect dots and a selection disc.
    /// </summary>
    /// <remarks>
    /// The view owns its own animation state. The simulation reports facts through
    /// <see cref="RtsBattleFeed"/> — "this unit swung", "this unit was hit" — and the flash timers below are
    /// the view's answer to them. That is why nothing in <see cref="RtsUnit"/> knows what a flash is.
    /// </remarks>
    public sealed class RtsUnitView
    {
        private static readonly Color BarBackground = new(0.06f, 0.07f, 0.09f);
        private static readonly Color BuffColor = new(0.45f, 0.9f, 0.5f);
        private static readonly Color DebuffColor = new(0.75f, 0.45f, 0.95f);
        private static readonly Color SelectionColor = new(1f, 0.85f, 0.35f);
        private static readonly Color HealthLow = new(0.9f, 0.28f, 0.28f);
        private static readonly Color HealthMid = new(0.95f, 0.8f, 0.25f);
        private static readonly Color HealthHigh = new(0.4f, 0.85f, 0.4f);

        private const float FlashDecay = 5f;

        private readonly GameObject _root;
        private readonly Transform _overlay;
        private readonly Transform _body;
        private readonly Material _bodyMaterial;
        private readonly Transform _barFill;
        private readonly Material _barFillMaterial;
        private readonly Transform _buffDot;
        private readonly Transform _debuffDot;
        private readonly Transform _selection;

        private readonly Color _baseColor;
        private readonly float _barWidth;
        private readonly float _bodyHeight;

        private float _swingFlash;
        private float _hitFlash;

        public RtsUnitView(Transform parent, RtsUnit unit, Color teamColor)
        {
            var archetype = unit.Archetype;

            _root = new GameObject($"unit-{unit.Id}-{archetype.Name}");
            _root.transform.SetParent(parent, false);

            _baseColor = archetype.IsHero
                ? Color.Lerp(teamColor, Color.white, 0.35f)
                : archetype.IsStructure
                    ? Color.Lerp(teamColor, new Color(0.25f, 0.26f, 0.3f), 0.45f)
                    : teamColor;

            _bodyHeight = archetype.Height;
            _barWidth = archetype.IsStructure ? 3.4f : 1.1f;
            _bodyMaterial = RtsPrimitives.NewUnlit(_baseColor);

            var shape = archetype.IsStructure ? PrimitiveType.Cube : PrimitiveType.Capsule;
            var body = RtsPrimitives.NewPrimitive(shape, _root.transform, _bodyMaterial);

            body.name = "body";

            body.transform.localScale = archetype.IsStructure
                ? new Vector3(archetype.Radius * 2f, archetype.Height, archetype.Radius * 2f)
                : new Vector3(archetype.Radius * 2f, archetype.Height * 0.5f, archetype.Radius * 2f);

            body.transform.localPosition = new Vector3(0f, archetype.Height * 0.5f, 0f);
            _body = body.transform;

            // Heroes get a disc under them: at a glance you can tell which blob is the one whose death costs
            // the army its aura.
            if (archetype.IsHero)
            {
                var disc = RtsPrimitives.NewPrimitive(
                      PrimitiveType.Cylinder
                    , _root.transform
                    , RtsPrimitives.NewUnlit(Color.Lerp(teamColor, Color.white, 0.6f)));

                disc.name = "hero-disc";
                disc.transform.localScale = new Vector3(archetype.Radius * 3f, 0.02f, archetype.Radius * 3f);
                disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            }

            _selection = RtsPrimitives.NewPrimitive(
                  PrimitiveType.Cylinder
                , _root.transform
                , RtsPrimitives.NewUnlit(SelectionColor)).transform;

            _selection.name = "selection";
            _selection.localScale = new Vector3(archetype.Radius * 3.6f, 0.015f, archetype.Radius * 3.6f);
            _selection.localPosition = new Vector3(0f, 0.03f, 0f);
            _selection.gameObject.SetActive(false);

            _overlay = new GameObject("overlay").transform;
            _overlay.SetParent(_root.transform, false);
            _overlay.localPosition = new Vector3(0f, archetype.Height + 0.45f, 0f);

            var back = RtsPrimitives.NewPrimitive(
                  PrimitiveType.Quad
                , _overlay
                , RtsPrimitives.NewUnlit(BarBackground)).transform;

            back.name = "hp-back";
            back.localScale = new Vector3(_barWidth, 0.17f, 1f);

            _barFillMaterial = RtsPrimitives.NewUnlit(HealthHigh);
            _barFill = RtsPrimitives.NewPrimitive(PrimitiveType.Quad, _overlay, _barFillMaterial).transform;
            _barFill.name = "hp-fill";

            _buffDot = NewDot("buff", BuffColor, -0.28f);
            _debuffDot = NewDot("debuff", DebuffColor, 0.28f);
        }

        private Transform NewDot(string name, Color color, float x)
        {
            var dot = RtsPrimitives.NewPrimitive(
                  PrimitiveType.Quad
                , _overlay
                , RtsPrimitives.NewUnlit(color)).transform;

            dot.name = name;
            dot.localScale = new Vector3(0.22f, 0.22f, 1f);
            dot.localPosition = new Vector3(x, 0.3f, -0.01f);
            dot.gameObject.SetActive(false);

            return dot;
        }

        /// <summary>A fact arrived from the simulation. How long it shows for is this view's business.</summary>
        public void Pulse(RtsBattleEventKind kind)
        {
            switch (kind)
            {
                case RtsBattleEventKind.Swing:
                    _swingFlash = 1f;
                    break;

                case RtsBattleEventKind.Hit:
                    _hitFlash = 1f;
                    break;
            }
        }

        public void Sync(
              RtsMatch match
            , RtsUnit unit
            , float alpha
            , float deltaTime
            , bool selected
            , Quaternion facing
        )
        {
            _swingFlash = Mathf.Max(_swingFlash - deltaTime * FlashDecay, 0f);
            _hitFlash = Mathf.Max(_hitFlash - deltaTime * FlashDecay, 0f);

            var position = Vector2.Lerp(
                  new Vector2(unit.PreviousPosition.x, unit.PreviousPosition.y)
                , new Vector2(unit.Position.x, unit.Position.y)
                , alpha);

            _root.transform.localPosition = new Vector3(position.x, 0f, position.y);

            SyncHealthBar(match.HpFraction(unit));

            RtsPrimitives.SetColor(_bodyMaterial, Color.Lerp(_baseColor, Color.white, _hitFlash * 0.75f));

            // A swing pushes the body up a little: enough to see who is fighting, without a single particle.
            _body.localPosition = new Vector3(0f, _bodyHeight * 0.5f * (1f + _swingFlash * 0.18f), 0f);

            _overlay.rotation = facing;
            _buffDot.gameObject.SetActive(match.Effects.HasBuff(unit));
            _debuffDot.gameObject.SetActive(match.Effects.HasDebuff(unit));
            _selection.gameObject.SetActive(selected);
        }

        private void SyncHealthBar(float fraction)
        {
            // Grow from the left: a quad's pivot is its centre, so the fill has to move as it scales.
            _barFill.localScale = new Vector3(Mathf.Max(_barWidth * fraction, 0.001f), 0.17f, 1f);
            _barFill.localPosition = new Vector3(-_barWidth * 0.5f * (1f - fraction), 0f, -0.01f);

            RtsPrimitives.SetColor(_barFillMaterial, fraction > 0.5f
                ? Color.Lerp(HealthMid, HealthHigh, (fraction - 0.5f) * 2f)
                : Color.Lerp(HealthLow, HealthMid, fraction * 2f));
        }

        public void Dispose() => RtsPrimitives.DestroyWithMaterials(_root);
    }
}
