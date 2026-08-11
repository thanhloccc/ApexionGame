using System.Runtime.CompilerServices;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// What a state behaviour is told about the moment it is running in.
    /// </summary>
    /// <remarks>
    /// Carries <see cref="NodeIndex"/> rather than the machine's state enum, because
    /// <see cref="StateBehaviour{TContext}"/> is generic over the context only. Use
    /// <c>MachineDefinition.StateOf(info.Node)</c> where the enum value is actually needed —
    /// which is rare, since a behaviour already knows which state it is.
    /// </remarks>
    public readonly struct StateInfo
    {
        /// <summary>The node this behaviour is attached to.</summary>
        public readonly NodeIndex Node;

        /// <summary>The leaf that was active before the current one, or <see cref="NodeIndex.Invalid"/>.</summary>
        public readonly NodeIndex PreviousNode;

        /// <summary>Seconds since this node was entered.</summary>
        public readonly float TimeInState;

        /// <summary>Seconds since the machine started.</summary>
        public readonly float TimeInMachine;

        /// <summary>The narrow view of the machine a behaviour is allowed to act on.</summary>
        public readonly IMachineControl Control;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateInfo(
              NodeIndex node
            , NodeIndex previousNode
            , float timeInState
            , float timeInMachine
            , IMachineControl control
        )
        {
            Node = node;
            PreviousNode = previousNode;
            TimeInState = timeInState;
            TimeInMachine = timeInMachine;
            Control = control;
        }
    }
}
