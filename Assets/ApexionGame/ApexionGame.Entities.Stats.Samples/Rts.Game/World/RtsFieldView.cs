using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>The ground, the centre line, and the circle that shows where a spell will land.</summary>
    public sealed class RtsFieldView
    {
        private readonly GameObject _root;
        private readonly GameObject _aimMarker;
        private readonly Material _aimMaterial;

        public RtsFieldView(Transform parent, float halfLength, float halfWidth)
        {
            _root = new GameObject("field");
            _root.transform.SetParent(parent, false);

            var ground = RtsPrimitives.NewPrimitive(
                  PrimitiveType.Cube
                , _root.transform
                , RtsPrimitives.NewUnlit(new Color(0.13f, 0.15f, 0.18f)));

            ground.name = "ground";
            ground.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3((halfLength + 3f) * 2f, 0.5f, halfWidth * 2f);

            var centre = RtsPrimitives.NewPrimitive(
                  PrimitiveType.Cube
                , _root.transform
                , RtsPrimitives.NewUnlit(new Color(0.2f, 0.22f, 0.26f)));

            centre.name = "centre-line";
            centre.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            centre.transform.localScale = new Vector3(0.12f, 0.02f, halfWidth * 2f);

            // Opaque, not translucent: URP's Unlit is an opaque surface until its material is configured
            // otherwise, and a sample that quietly relies on blend state it never set is a sample that breaks
            // in the next project.
            _aimMaterial = RtsPrimitives.NewUnlit(new Color(0.95f, 0.78f, 0.3f));
            _aimMarker = RtsPrimitives.NewPrimitive(PrimitiveType.Cylinder, _root.transform, _aimMaterial);
            _aimMarker.name = "aim-marker";
            _aimMarker.SetActive(false);
        }

        public void HideAim() => _aimMarker.SetActive(false);

        public void ShowAim(Vector2 groundPoint, float radius, bool hostile)
        {
            _aimMarker.SetActive(true);
            _aimMarker.transform.localPosition = new Vector3(groundPoint.x, 0.04f, groundPoint.y);
            _aimMarker.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

            RtsPrimitives.SetColor(_aimMaterial, hostile
                ? new Color(0.85f, 0.45f, 0.95f)
                : new Color(0.95f, 0.8f, 0.35f));
        }

        public void Dispose() => RtsPrimitives.DestroyWithMaterials(_root);
    }
}
