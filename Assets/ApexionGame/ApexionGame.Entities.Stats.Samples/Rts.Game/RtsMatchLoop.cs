using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// Turns frame time into whole simulation ticks, and reports how far between two of them the view is.
    /// </summary>
    /// <remarks>
    /// The simulation runs on a fixed step so it stays deterministic and testable; the view interpolates so it
    /// stays smooth. This little class is the whole seam between those two facts.
    /// </remarks>
    public sealed class RtsMatchLoop
    {
        /// <summary>
        /// A frame that fell behind runs at most this many ticks, then drops the rest.
        /// </summary>
        /// <remarks>
        /// Without a cap, one long frame makes the next frame longer — the classic simulation spiral. Dropping
        /// simulated time is the right trade for a sample: the match slows down instead of locking up.
        /// </remarks>
        private const int MaxTicksPerFrame = 8;

        private readonly float _tickSeconds;

        private float _accumulator;

        public RtsMatchLoop(float tickSeconds)
        {
            _tickSeconds = tickSeconds > 0f ? tickSeconds : 0.05f;
        }

        public bool Paused { get; private set; }

        public float Speed { get; private set; } = 1f;

        /// <summary>How far the view is between the last tick and the next one, 0..1.</summary>
        public float Alpha => Paused ? 1f : Mathf.Clamp01(_accumulator / _tickSeconds);

        public void TogglePause() => Paused = Paused == false;

        public void SetSpeed(float speed) => Speed = Mathf.Clamp(speed, 0.25f, 8f);

        public void Reset() => _accumulator = 0f;

        /// <summary>Runs however many whole ticks this frame earned. Returns how many it ran.</summary>
        public int Advance(RtsMatch match, float deltaTime)
        {
            if (Paused || match.IsOver)
            {
                return 0;
            }

            _accumulator += deltaTime * Speed;

            var ticks = 0;

            while (_accumulator >= _tickSeconds && ticks < MaxTicksPerFrame)
            {
                match.Tick();
                _accumulator -= _tickSeconds;
                ticks++;
            }

            if (_accumulator > _tickSeconds)
            {
                // Behind by more than the budget: throw the surplus away rather than compound it next frame.
                _accumulator = _tickSeconds * 0.999f;
            }

            return ticks;
        }
    }
}
