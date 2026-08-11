using System;
using System.Collections.Generic;

namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// Where live machines announce themselves so tooling can find them.
    /// </summary>
    /// <remarks>
    /// Unlike the stats module, registration here is <b>automatic</b>. A machine is an ordinary
    /// managed object that implements <see cref="IMachineDebug"/> itself, so there is no adapter to
    /// construct and a stale registration is a leak rather than a dangling pointer — and the
    /// registering call is <c>[Conditional]</c>, so a release build does not make it at all.
    /// <para>
    /// Nothing in the runtime reads this registry.
    /// </para>
    /// </remarks>
    public static class MachineDebugRegistry
    {
        private static readonly List<IMachineDebug> s_machines = new();

        /// <summary>
        /// Raised when a machine is registered or unregistered, so a window can refresh its list.
        /// </summary>
        public static event Action Changed;

        public static IReadOnlyList<IMachineDebug> Machines => s_machines;

        public static void Register(IMachineDebug machine)
        {
            if (machine == null || s_machines.Contains(machine))
            {
                return;
            }

            s_machines.Add(machine);
            Changed?.Invoke();
        }

        public static void Unregister(IMachineDebug machine)
        {
            if (machine == null || s_machines.Remove(machine) == false)
            {
                return;
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// Drops registrations whose machine has been disposed without unregistering.
        /// </summary>
        /// <remarks>
        /// A window should call this before reading <see cref="Machines"/>, so a machine dropped on
        /// the floor disappears instead of lingering as a ghost.
        /// </remarks>
        public static void PruneDead()
        {
            var removed = false;

            for (var i = s_machines.Count - 1; i >= 0; i--)
            {
                if (s_machines[i].IsAlive == false)
                {
                    s_machines.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Drops every registration. Domain reload does this for free.
        /// </summary>
        public static void Clear()
        {
            if (s_machines.Count == 0)
            {
                return;
            }

            s_machines.Clear();
            Changed?.Invoke();
        }
    }
}
