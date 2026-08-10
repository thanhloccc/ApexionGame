using System;
using EncosyTower.Common;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// The slice of a machine a state behaviour is allowed to touch.
    /// </summary>
    /// <remarks>
    /// A behaviour never holds the machine itself, only this. That is what keeps
    /// <see cref="StateBehaviour{TContext}"/> free of the state enum, and therefore reusable across
    /// machines whose states are different enums.
    /// </remarks>
    public interface IMachineControl
    {
        /// <summary>
        /// Seconds since the machine started, across every state it has been in.
        /// </summary>
        float TimeInMachine { get; }

        MachinePhase Phase { get; }

        /// <summary>
        /// Queues a trigger. It is drained at a defined point in the next tick, never applied
        /// synchronously from inside a behaviour.
        /// </summary>
        void Fire<TTrigger>(TTrigger trigger) where TTrigger : unmanaged, Enum;

        /// <inheritdoc cref="Fire{TTrigger}(TTrigger)"/>
        /// <returns>The queued id, or why it could not be queued.</returns>
        Result<TriggerId, MachineError> FireOrError<TTrigger>(TTrigger trigger)
            where TTrigger : unmanaged, Enum;

        /// <summary>
        /// Asks for a transition to <paramref name="target"/> without going through a guard or a
        /// trigger.
        /// </summary>
        /// <returns>The resolved target node, or why the request was refused.</returns>
        Result<NodeIndex, MachineError> RequestTransitionOrError(NodeIndex target);
    }
}
