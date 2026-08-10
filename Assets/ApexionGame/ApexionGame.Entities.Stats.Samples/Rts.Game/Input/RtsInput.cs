using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// One frame of input, sampled straight from the Input System devices.
    /// </summary>
    /// <remarks>
    /// This project is configured with <c>activeInputHandler: 1</c> — the Input System package only — so the
    /// legacy <c>UnityEngine.Input</c> class does not work here at all. Six keys and a mouse do not justify an
    /// InputActions asset, and every device read is null-guarded so a headless run does not throw.
    /// </remarks>
    public sealed class RtsInput
    {
        public Vector2 Pointer;
        public Vector2 PointerDelta;
        public bool LeftPressed;
        public bool RightHeld;
        public float Scroll;

        /// <summary>Arrow keys only: WASD would collide with the spawn keys.</summary>
        public Vector2 Pan;

        public bool PausePressed;
        public bool CancelPressed;
        public bool RestartPressed;

        /// <summary>Index of the spell key pressed this frame, or -1.</summary>
        public int SpellPressed;

        /// <summary>Index into the team's spawn menu pressed this frame, or -1.</summary>
        public int SpawnPressed;

        public void Sample()
        {
            LeftPressed = false;
            RightHeld = false;
            Scroll = 0f;
            PointerDelta = Vector2.zero;
            Pan = Vector2.zero;
            PausePressed = false;
            CancelPressed = false;
            RestartPressed = false;
            SpellPressed = -1;
            SpawnPressed = -1;

            SampleMouse(Mouse.current);
            SampleKeyboard(Keyboard.current);
        }

        private void SampleMouse(Mouse mouse)
        {
            if (mouse == null)
            {
                return;
            }

            Pointer = mouse.position.ReadValue();
            PointerDelta = mouse.delta.ReadValue();
            LeftPressed = mouse.leftButton.wasPressedThisFrame;
            RightHeld = mouse.rightButton.isPressed;
            Scroll = mouse.scroll.ReadValue().y;
        }

        private void SampleKeyboard(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) SpellPressed = 0;
            if (keyboard.digit2Key.wasPressedThisFrame) SpellPressed = 1;
            if (keyboard.digit3Key.wasPressedThisFrame) SpellPressed = 2;
            if (keyboard.digit4Key.wasPressedThisFrame) SpellPressed = 3;
            if (keyboard.digit5Key.wasPressedThisFrame) SpellPressed = 4;
            if (keyboard.digit6Key.wasPressedThisFrame) SpellPressed = 5;

            if (keyboard.qKey.wasPressedThisFrame) SpawnPressed = 0;
            if (keyboard.wKey.wasPressedThisFrame) SpawnPressed = 1;
            if (keyboard.eKey.wasPressedThisFrame) SpawnPressed = 2;
            if (keyboard.rKey.wasPressedThisFrame) SpawnPressed = 3;

            PausePressed = keyboard.spaceKey.wasPressedThisFrame;
            CancelPressed = keyboard.escapeKey.wasPressedThisFrame;
            RestartPressed = keyboard.f5Key.wasPressedThisFrame;

            var x = 0f;
            var y = 0f;

            if (keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.upArrowKey.isPressed) y += 1f;

            Pan = new Vector2(x, y);
        }
    }
}
