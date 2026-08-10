using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// A machine's baked shape: built once, shared by every instance of it, never mutated
    /// afterwards.
    /// </summary>
    /// <remarks>
    /// This is the flyweight half of the design. Spawning an agent allocates one small instance
    /// against this, not a tree of state objects — which is what lets one definition serve hundreds
    /// of agents.
    /// </remarks>
    public sealed class MachineDefinition<TContext, TState>
        where TContext : class
        where TState : unmanaged, Enum
    {
        internal readonly StateNode[] Nodes;
        internal readonly Transition[] Transitions;
        internal readonly StateBehaviour<TContext>[] Behaviours;
        internal readonly object[] Guards;
        internal readonly string[] GuardExpressions;
        internal readonly TransitionAction<TContext>[] Actions;

        /// <summary>
        /// How many entries at the head of <see cref="Transitions"/> are any-state transitions.
        /// They are contiguous and first, so they are evaluated with one linear walk.
        /// </summary>
        internal readonly int AnyStateCount;

        private readonly int[] _ordinalToNode;
        private readonly TState[] _nodeToState;
        private readonly Stack<HierarchicalStateMachine<TContext, TState>> _pool = new();

        private int _nextInstanceId;

        internal MachineDefinition(
              string name
            , StateNode[] nodes
            , Transition[] transitions
            , int anyStateCount
            , StateBehaviour<TContext>[] behaviours
            , object[] guards
            , string[] guardExpressions
            , TransitionAction<TContext>[] actions
            , int[] ordinalToNode
            , TState[] nodeToState
            , int stateDataSize
        )
        {
            Name = name;
            Nodes = nodes;
            Transitions = transitions;
            AnyStateCount = anyStateCount;
            Behaviours = behaviours;
            Guards = guards;
            GuardExpressions = guardExpressions;
            Actions = actions;
            StateDataSize = stateDataSize;
            _ordinalToNode = ordinalToNode;
            _nodeToState = nodeToState;
        }

        public string Name { get; }

        public int NodeCount => Nodes.Length;

        public int TransitionCount => Transitions.Length;

        /// <summary>
        /// Bytes each instance allocates for the behaviours' state-data slots.
        /// </summary>
        public int StateDataSize { get; }

        /// <summary>
        /// Machines currently sitting in the pool, waiting to be handed out again.
        /// </summary>
        public int PooledCount => _pool.Count;

        /// <summary>
        /// Creates an instance ticked by <see cref="MachineRunner.Default"/>, started immediately.
        /// </summary>
        public HierarchicalStateMachine<TContext, TState> CreateInstance(TContext context)
            => CreateInstance(context, default(MachineOptions));

        /// <inheritdoc cref="CreateInstance(TContext)"/>
        public HierarchicalStateMachine<TContext, TState> CreateInstance(TContext context, TickMode tickMode)
            => CreateInstance(context, new MachineOptions { TickMode = tickMode });

        /// <inheritdoc cref="CreateInstance(TContext)"/>
        public HierarchicalStateMachine<TContext, TState> CreateInstance(
              TContext context
            , in MachineOptions options
        )
        {
            var instance = _pool.Count > 0 ? _pool.Pop() : new HierarchicalStateMachine<TContext, TState>(this);
            instance.Initialize(context, options, _nextInstanceId++);
            return instance;
        }

        /// <summary>
        /// The node a state was declared as, or <see cref="NodeIndex.Invalid"/> if it never was.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NodeIndex IndexOf(TState state)
        {
            var ordinal = Internals.EnumOrdinal.Of(state);
            return (uint)ordinal < (uint)_ordinalToNode.Length
                ? NodeIndex.Of(_ordinalToNode[ordinal])
                : NodeIndex.Invalid;
        }

        /// <summary>
        /// The state a node was declared with. The root has no state and returns
        /// <see langword="default"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TState StateOf(NodeIndex node)
            => node.IsWithin(_nodeToState.Length) ? _nodeToState[node.value] : default;

        /// <summary>
        /// A node's display name. Allocates, so it belongs in tooling and log messages only.
        /// </summary>
        public string NameOf(NodeIndex node)
        {
            if (node.IsWithin(Nodes.Length) == false)
            {
                return "<invalid>";
            }

            return node.value == MachineBuilder<TContext, TState>.ROOT
                ? "Root"
                : _nodeToState[node.value].ToString();
        }

        internal void Return(HierarchicalStateMachine<TContext, TState> instance)
            => _pool.Push(instance);

        /// <summary>
        /// Drops every pooled instance. The pool is a cache, so this only frees memory.
        /// </summary>
        public void ClearPool()
            => _pool.Clear();
    }
}
