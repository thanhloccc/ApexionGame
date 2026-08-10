using System;
using EncosyTower.Collections;
using EncosyTower.Logging;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// Ticks a batch of machines in one pass.
    /// </summary>
    /// <remarks>
    /// The default tick path for every instance created with <see cref="TickMode.Runner"/>. One
    /// runner ticking five hundred machines is one cache-friendly loop instead of five hundred
    /// separate <c>Update</c> calls scattered across the frame.
    /// </remarks>
    public sealed class MachineRunner
    {
        private static MachineRunner s_default;

        private readonly FasterList<IMachineTickable> _machines = new();
        private readonly FasterList<IMachineTickable> _pendingAdd = new();
        private readonly FasterList<IMachineTickable> _pendingRemove = new();

        private bool _isTicking;

        public MachineRunner(string name)
        {
            Name = string.IsNullOrEmpty(name) ? "MachineRunner" : name;
        }

        /// <summary>
        /// The runner every instance uses unless it is created with a different one, or with
        /// <see cref="TickMode.Manual"/>.
        /// </summary>
        public static MachineRunner Default => s_default ??= new MachineRunner("Default");

        public string Name { get; }

        public int Count => _machines.Count;

        /// <summary>
        /// Ticks every registered machine once.
        /// </summary>
        /// <remarks>
        /// Registering or unregistering a machine from inside a machine's own <c>Tick</c> — the
        /// common case being a machine that disposes itself on entering a terminal state — is safe:
        /// both are deferred to the end of this pass. Removal is a swap-with-last, so tick order is
        /// not stable across a removal; nothing may depend on it.
        /// <para>
        /// A machine whose <c>Tick</c> throws is caught, logged, and unregistered, so one broken
        /// agent does not take the whole frame down with it every frame after.
        /// </para>
        /// </remarks>
        public void Tick(float deltaTime)
        {
            _isTicking = true;

            for (var i = 0; i < _machines.Count; i++)
            {
                var machine = _machines[i];

                try
                {
                    machine.Tick(deltaTime);
                }
                catch (Exception exception)
                {
                    DevLogger.Default.LogError(
                        $"[{Name}] '{machine.DebugName}' threw during Tick and has been unregistered."
                    );
                    DevLogger.Default.LogException(exception);
                    _pendingRemove.Add(machine);
                }
            }

            _isTicking = false;
            ApplyPending();
        }

        /// <summary>
        /// Unregisters every machine without disposing them. The machines themselves are untouched.
        /// </summary>
        public void Clear()
        {
            _machines.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
        }

        internal void Register(IMachineTickable machine)
        {
            if (_isTicking)
            {
                _pendingAdd.Add(machine);
                return;
            }

            _machines.Add(machine);
        }

        internal void Unregister(IMachineTickable machine)
        {
            if (_isTicking)
            {
                _pendingRemove.Add(machine);
                return;
            }

            RemoveNow(machine);
        }

        private void ApplyPending()
        {
            for (var i = 0; i < _pendingRemove.Count; i++)
            {
                RemoveNow(_pendingRemove[i]);
            }

            _pendingRemove.Clear();

            for (var i = 0; i < _pendingAdd.Count; i++)
            {
                _machines.Add(_pendingAdd[i]);
            }

            _pendingAdd.Clear();
        }

        private void RemoveNow(IMachineTickable machine)
        {
            var index = _machines.IndexOf(machine);

            if (index >= 0)
            {
                _machines.RemoveAtSwapBack(index);
            }
        }

        // ── player loop ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Inserts <see cref="Default"/> as a subsystem of Unity's <c>Update</c> phase, so it ticks
        /// without a <see cref="MachineRunnerBehaviour"/> in any scene.
        /// </summary>
        /// <remarks>
        /// Idempotent — calling this twice does not insert a second copy.
        /// </remarks>
        public static void InstallIntoPlayerLoop()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            var system = new PlayerLoopSystem {
                type = typeof(MachineRunner),
                updateDelegate = TickDefault,
            };

            if (TryInsertUnderUpdate(ref loop, system))
            {
                PlayerLoop.SetPlayerLoop(loop);
            }
        }

        public static void RemoveFromPlayerLoop()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            if (TryRemoveFromUpdate(ref loop, typeof(MachineRunner)))
            {
                PlayerLoop.SetPlayerLoop(loop);
            }
        }

        private static void TickDefault()
            => s_default?.Tick(UnityEngine.Time.deltaTime);

        private static bool TryInsertUnderUpdate(ref PlayerLoopSystem root, in PlayerLoopSystem system)
        {
            var phases = root.subSystemList;

            if (phases == null)
            {
                return false;
            }

            for (var i = 0; i < phases.Length; i++)
            {
                if (phases[i].type != typeof(Update))
                {
                    continue;
                }

                var updatePhase = phases[i];
                var children = updatePhase.subSystemList ?? Array.Empty<PlayerLoopSystem>();

                for (var j = 0; j < children.Length; j++)
                {
                    if (children[j].type == system.type)
                    {
                        return false;
                    }
                }

                var withSystem = new PlayerLoopSystem[children.Length + 1];
                Array.Copy(children, withSystem, children.Length);
                withSystem[children.Length] = system;

                updatePhase.subSystemList = withSystem;
                phases[i] = updatePhase;
                return true;
            }

            return false;
        }

        private static bool TryRemoveFromUpdate(ref PlayerLoopSystem root, Type systemType)
        {
            var phases = root.subSystemList;

            if (phases == null)
            {
                return false;
            }

            for (var i = 0; i < phases.Length; i++)
            {
                if (phases[i].type != typeof(Update))
                {
                    continue;
                }

                var updatePhase = phases[i];
                var children = updatePhase.subSystemList;

                if (children == null)
                {
                    return false;
                }

                var removeAt = -1;

                for (var j = 0; j < children.Length; j++)
                {
                    if (children[j].type == systemType)
                    {
                        removeAt = j;
                        break;
                    }
                }

                if (removeAt < 0)
                {
                    return false;
                }

                var withoutSystem = new PlayerLoopSystem[children.Length - 1];
                Array.Copy(children, 0, withoutSystem, 0, removeAt);
                Array.Copy(children, removeAt + 1, withoutSystem, removeAt, children.Length - removeAt - 1);

                updatePhase.subSystemList = withoutSystem;
                phases[i] = updatePhase;
                return true;
            }

            return false;
        }
    }
}
