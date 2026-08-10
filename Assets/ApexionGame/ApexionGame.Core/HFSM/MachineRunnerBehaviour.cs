using UnityEngine;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// Which clock a <see cref="MachineRunnerBehaviour"/> ticks its runner with.
    /// </summary>
    /// <remarks>
    /// Kept in this file rather than its own: it exists only to be this component's one
    /// serialized field, and a one-line enum in its own file would be noise.
    /// </remarks>
    public enum RunnerDeltaTime : byte
    {
        Scaled = 0,
        Unscaled = 1,
        Fixed = 2,
    }

    /// <summary>
    /// The scene-based alternative to <see cref="MachineRunner.InstallIntoPlayerLoop"/>: drop this
    /// into a scene and its runner ticks every frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MachineRunnerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private RunnerDeltaTime _deltaTime = RunnerDeltaTime.Scaled;

        private MachineRunner _runner;

        /// <summary>
        /// The runner this component ticks. <see cref="MachineRunner.Default"/> unless
        /// <see cref="SetRunner"/> was called.
        /// </summary>
        public MachineRunner Runner => _runner ??= MachineRunner.Default;

        public void SetRunner(MachineRunner runner)
            => _runner = runner;

        private void Update()
        {
            if (_deltaTime == RunnerDeltaTime.Scaled)
            {
                Runner.Tick(Time.deltaTime);
            }
            else if (_deltaTime == RunnerDeltaTime.Unscaled)
            {
                Runner.Tick(Time.unscaledDeltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (_deltaTime == RunnerDeltaTime.Fixed)
            {
                Runner.Tick(Time.fixedDeltaTime);
            }
        }
    }
}
