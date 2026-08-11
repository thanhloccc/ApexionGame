using Unity.Mathematics;
using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// An orthographic camera looking down at the field, with pan and zoom.
    /// </summary>
    /// <remarks>
    /// The rig configures whatever camera it is handed rather than requiring a set-up one, so the scene stays a
    /// camera plus one GameObject and there is nothing to get wrong in the Inspector.
    /// </remarks>
    public sealed class RtsCameraRig
    {
        private const float Pitch = 52f;
        private const float Distance = 60f;
        private const float MinSize = 7f;
        private const float MaxSize = 26f;
        private const float DefaultSize = 15f;

        private readonly Camera _camera;
        private readonly float _panLimitX;
        private readonly float _panLimitZ;

        private Vector3 _focus;
        private float _size = DefaultSize;

        public RtsCameraRig(Camera camera, float fieldHalfLength, float laneHalfWidth)
        {
            _camera = camera;
            _panLimitX = fieldHalfLength + 4f;
            _panLimitZ = laneHalfWidth + 4f;

            _camera.orthographic = true;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 400f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            _camera.transform.rotation = Quaternion.Euler(Pitch, 0f, 0f);

            Apply();
        }

        /// <summary>The rotation a world-space label has to take to face this camera.</summary>
        public Quaternion Facing => _camera.transform.rotation;

        public void Frame()
        {
            _focus = Vector3.zero;
            _size = DefaultSize;
            Apply();
        }

        public void Update(RtsInput input, float deltaTime, bool pointerOverUi)
        {
            if (input.Pan.sqrMagnitude > 0f)
            {
                _focus += new Vector3(input.Pan.x, 0f, input.Pan.y) * (_size * 4.2f * deltaTime);
            }

            if (input.RightHeld)
            {
                // Screen pixels to world metres: an orthographic camera shows 2 × size vertically.
                var scale = _size * 2f / Mathf.Max(Screen.height, 1);
                _focus -= new Vector3(input.PointerDelta.x, 0f, input.PointerDelta.y) * scale;
            }

            if (pointerOverUi == false && Mathf.Abs(input.Scroll) > 0.01f)
            {
                _size = Mathf.Clamp(_size - Mathf.Sign(input.Scroll) * _size * 0.12f, MinSize, MaxSize);
            }

            _focus.x = Mathf.Clamp(_focus.x, -_panLimitX, _panLimitX);
            _focus.z = Mathf.Clamp(_focus.z, -_panLimitZ, _panLimitZ);
            _focus.y = 0f;

            Apply();
        }

        private void Apply()
        {
            _camera.orthographicSize = _size;
            _camera.transform.position = _focus - _camera.transform.forward * Distance;
        }

        /// <summary>Where a screen point lands on the ground plane.</summary>
        public float2 ScreenToGround(Vector2 screenPosition)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);

            if (Mathf.Abs(ray.direction.y) < 1e-4f)
            {
                return new float2(_focus.x, _focus.z);
            }

            var point = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);

            return new float2(point.x, point.z);
        }
    }
}
