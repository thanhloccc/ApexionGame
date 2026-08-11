using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ApexionGame.HFSM.Internals;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// Declares a machine's shape, then bakes it into a
    /// <see cref="MachineDefinition{TContext, TState}"/>.
    /// </summary>
    /// <remarks>
    /// Every method returns the builder, including the transition modifiers, so a declaration is one
    /// chain. A modifier called with no transition pending, or an <c>End*</c> with no matching
    /// scope, is recorded and reported by <see cref="BuildOrError"/> rather than throwing where it
    /// happened — the first error wins, so the message points at the cause and not at a knock-on.
    /// <para>
    /// Build is a cold path that runs once at startup. It uses plain
    /// <see cref="List{T}"/> deliberately; the baked definition and the tick path use arrays.
    /// </para>
    /// </remarks>
    public sealed partial class MachineBuilder<TContext, TState>
        where TContext : class
        where TState : unmanaged, Enum
    {
        /// <summary>
        /// The implicit container every top-level state is declared into. Always node 0.
        /// </summary>
        internal const int ROOT = 0;

        private readonly string _name;
        private readonly List<NodeDraft> _nodes = new();
        private readonly List<TransitionDraft> _transitions = new();
        private readonly List<StateBehaviour<TContext>> _behaviours = new();
        private readonly List<object> _guards = new();
        private readonly List<string> _guardExpressions = new();
        private readonly List<TransitionAction<TContext>> _actions = new();
        private readonly Dictionary<int, int> _ordinalToDraft = new();
        private readonly List<int> _scopes = new();

        private int _cursor = ROOT;
        private int _pendingTransition = -1;
        private bool _anyStatePending;
        private bool _hasError;
        private MachineError _error;

        internal MachineBuilder(string name)
        {
            _name = string.IsNullOrEmpty(name) ? "Machine" : name;

            _nodes.Add(new NodeDraft {
                Parent = -1,
                Ordinal = -1,
                Kind = StateNodeKind.Composite,
                History = HistoryMode.None,
                InitialOrdinal = -1,
                BehaviourIndex = -1,
            });
        }

        // ── nodes ───────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Declares a leaf state whose behaviour is constructed once and shared by every instance.
        /// </summary>
        public MachineBuilder<TContext, TState> State<TBehaviour>(TState state)
            where TBehaviour : StateBehaviour<TContext>, new()
            => Declare(state, StateNodeKind.Leaf, AddBehaviour(new TBehaviour()));

        /// <summary>
        /// Declares a leaf state with no behaviour — a state the machine can be in, that does
        /// nothing on its own.
        /// </summary>
        public MachineBuilder<TContext, TState> State(TState state)
            => Declare(state, StateNodeKind.Leaf, -1);

        /// <summary>
        /// Declares a leaf state sharing an already-constructed behaviour instance.
        /// </summary>
        /// <remarks>
        /// Placing one instance at two nodes gives it two independent state-data slots.
        /// </remarks>
        public MachineBuilder<TContext, TState> State(TState state, StateBehaviour<TContext> behaviour)
            => Declare(state, StateNodeKind.Leaf, AddBehaviour(behaviour));

        /// <inheritdoc cref="State{TBehaviour}(TState)"/>
        /// <remarks>Reads better inside a composite. Identical to <see cref="State{TBehaviour}"/>.</remarks>
        public MachineBuilder<TContext, TState> Child<TBehaviour>(TState state)
            where TBehaviour : StateBehaviour<TContext>, new()
            => State<TBehaviour>(state);

        /// <inheritdoc cref="Child{TBehaviour}(TState)"/>
        public MachineBuilder<TContext, TState> Child(TState state)
            => State(state);

        /// <inheritdoc cref="Child{TBehaviour}(TState)"/>
        public MachineBuilder<TContext, TState> Child(TState state, StateBehaviour<TContext> behaviour)
            => State(state, behaviour);

        /// <summary>
        /// Opens a composite: exactly one child is active at a time. Close it with
        /// <see cref="EndComposite"/>.
        /// </summary>
        public MachineBuilder<TContext, TState> Composite(TState state)
        {
            Declare(state, StateNodeKind.Composite, -1);
            PushScope();
            return this;
        }

        /// <summary>
        /// Opens a composite with a behaviour of its own, which enters before its children and exits
        /// after them.
        /// </summary>
        public MachineBuilder<TContext, TState> Composite<TBehaviour>(TState state)
            where TBehaviour : StateBehaviour<TContext>, new()
        {
            Declare(state, StateNodeKind.Composite, AddBehaviour(new TBehaviour()));
            PushScope();
            return this;
        }

        /// <inheritdoc cref="Composite{TBehaviour}(TState)"/>
        public MachineBuilder<TContext, TState> Composite(TState state, StateBehaviour<TContext> behaviour)
        {
            Declare(state, StateNodeKind.Composite, AddBehaviour(behaviour));
            PushScope();
            return this;
        }

        /// <summary>
        /// Opens a parallel node: every child region is active at once. Close it with
        /// <see cref="EndParallel"/>.
        /// </summary>
        public MachineBuilder<TContext, TState> Parallel(TState state)
        {
            Declare(state, StateNodeKind.Parallel, -1);
            PushScope();
            return this;
        }

        /// <inheritdoc cref="Parallel(TState)"/>
        public MachineBuilder<TContext, TState> Parallel(TState state, StateBehaviour<TContext> behaviour)
        {
            Declare(state, StateNodeKind.Parallel, AddBehaviour(behaviour));
            PushScope();
            return this;
        }

        /// <summary>
        /// Opens one branch of the enclosing parallel node. Close it with <see cref="EndRegion"/>.
        /// </summary>
        public MachineBuilder<TContext, TState> Region(TState state)
        {
            if (CurrentScopeKind() != StateNodeKind.Parallel)
            {
                return Fail(MachineError.UnbalancedScope(NodeIndex.Of(_cursor), _scopes.Count));
            }

            Declare(state, StateNodeKind.Region, -1);
            PushScope();
            return this;
        }

        /// <summary>
        /// Names the child entered when no history applies. Without it, the first declared child is
        /// used.
        /// </summary>
        /// <remarks>
        /// Called outside any scope, it names the machine's starting state.
        /// </remarks>
        public MachineBuilder<TContext, TState> Initial(TState state)
        {
            var node = _scopes.Count > 0 ? _scopes[^1] : ROOT;
            var draft = _nodes[node];
            draft.InitialOrdinal = EnumOrdinal.Of(state);
            _nodes[node] = draft;
            return this;
        }

        /// <summary>
        /// Makes the open container remember where it was when it is left.
        /// </summary>
        public MachineBuilder<TContext, TState> WithHistory(HistoryMode history)
        {
            var node = _scopes.Count > 0 ? _scopes[^1] : _cursor;

            if (node < 0 || node >= _nodes.Count || _nodes[node].Kind == StateNodeKind.Leaf)
            {
                return Fail(MachineError.UnbalancedScope(NodeIndex.Of(_cursor), _scopes.Count));
            }

            var draft = _nodes[node];
            draft.History = history;
            _nodes[node] = draft;
            return this;
        }

        public MachineBuilder<TContext, TState> EndComposite()
            => PopScope(StateNodeKind.Composite);

        public MachineBuilder<TContext, TState> EndRegion()
            => PopScope(StateNodeKind.Region);

        public MachineBuilder<TContext, TState> EndParallel()
            => PopScope(StateNodeKind.Parallel);

        // ── transitions ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Makes the next <see cref="To"/> leave every state rather than the one just declared.
        /// </summary>
        /// <remarks>
        /// Any-state transitions are evaluated before any node's own, because interrupting from
        /// anywhere is the only reason they exist.
        /// </remarks>
        public MachineBuilder<TContext, TState> AnyState()
        {
            _anyStatePending = true;
            return this;
        }

        /// <summary>
        /// Opens a transition from the state just declared — or from any state, after
        /// <see cref="AnyState"/>.
        /// </summary>
        public MachineBuilder<TContext, TState> To(TState target)
        {
            var source = _anyStatePending ? -1 : _cursor;
            _anyStatePending = false;

            _transitions.Add(new TransitionDraft {
                SourceDraft = source,
                TargetOrdinal = EnumOrdinal.Of(target),
                GuardIndex = -1,
                ActionIndex = -1,
                Trigger = TriggerId.None,
                MinDuration = 0f,
                Priority = 0,
                Flags = TransitionFlags.None,
                Order = _transitions.Count,
            });

            _pendingTransition = _transitions.Count - 1;
            return this;
        }

        /// <summary>
        /// Guards the open transition on a plain read of the blackboard.
        /// </summary>
        /// <param name="expression">
        /// Filled in by the compiler with the literal source text of <paramref name="guard"/>, which
        /// is what the debugger's guard pane shows. Never pass it explicitly.
        /// </param>
        public MachineBuilder<TContext, TState> When(
              Guard<TContext> guard
            , [CallerArgumentExpression("guard")] string expression = null
        )
        {
            if (TryGetPending(out var index) == false)
            {
                return this;
            }

            var draft = _transitions[index];
            draft.GuardIndex = AddGuard(guard, expression);
            draft.Flags &= ~TransitionFlags.GuardTakesInfo;
            _transitions[index] = draft;
            return this;
        }

        /// <summary>
        /// Guards the open transition on the blackboard plus timing and the source node.
        /// </summary>
        /// <inheritdoc cref="When(Guard{TContext}, string)"/>
        public MachineBuilder<TContext, TState> When(
              GuardWithInfo<TContext> guard
            , [CallerArgumentExpression("guard")] string expression = null
        )
        {
            if (TryGetPending(out var index) == false)
            {
                return this;
            }

            var draft = _transitions[index];
            draft.GuardIndex = AddGuard(guard, expression);
            draft.Flags |= TransitionFlags.GuardTakesInfo;
            _transitions[index] = draft;
            return this;
        }

        /// <summary>
        /// Arms the open transition on a trigger instead of polling it.
        /// </summary>
        public MachineBuilder<TContext, TState> On<TTrigger>(TTrigger trigger)
            where TTrigger : unmanaged, Enum
        {
            if (TryGetPending(out var index) == false)
            {
                return this;
            }

            var draft = _transitions[index];
            draft.Trigger = TriggerId.Of(trigger);
            _transitions[index] = draft;
            return this;
        }

        /// <summary>
        /// Fires the open transition once the source has been active for <paramref name="seconds"/>.
        /// </summary>
        public MachineBuilder<TContext, TState> After(float seconds)
        {
            if (TryGetPending(out var index) == false)
            {
                return this;
            }

            var draft = _transitions[index];
            draft.MinDuration = seconds;
            draft.Flags |= TransitionFlags.Timed;
            _transitions[index] = draft;
            return this;
        }

        /// <summary>
        /// Refuses the open transition until the source has been active for
        /// <paramref name="seconds"/>, whatever else makes it eligible.
        /// </summary>
        public MachineBuilder<TContext, TState> MinDuration(float seconds)
        {
            if (TryGetPending(out var index) == false)
            {
                return this;
            }

            var draft = _transitions[index];
            draft.MinDuration = seconds;
            _transitions[index] = draft;
            return this;
        }

        /// <summary>
        /// Orders the open transition against its siblings. Higher goes first; the default is 0, and
        /// ties fall back to declaration order.
        /// </summary>
        public MachineBuilder<TContext, TState> Priority(int priority)
        {
            if (TryGetPending(out var index) == false)
            {
                return this;
            }

            var draft = _transitions[index];
            draft.Priority = priority;
            _transitions[index] = draft;
            return this;
        }

        /// <summary>
        /// Runs the open transition's action without exiting or entering anything — react without
        /// restarting.
        /// </summary>
        public MachineBuilder<TContext, TState> Internal()
        {
            if (TryGetPending(out var index) == false)
            {
                return this;
            }

            var draft = _transitions[index];
            draft.Flags |= TransitionFlags.Internal;
            _transitions[index] = draft;
            return this;
        }

        /// <summary>
        /// Runs <paramref name="action"/> between the exit chain and the enter chain.
        /// </summary>
        public MachineBuilder<TContext, TState> Do(TransitionAction<TContext> action)
        {
            if (TryGetPending(out var index) == false)
            {
                return this;
            }

            var draft = _transitions[index];
            _actions.Add(action);
            draft.ActionIndex = _actions.Count - 1;
            _transitions[index] = draft;
            return this;
        }

        // ── internals ───────────────────────────────────────────────────────────────────────────

        private MachineBuilder<TContext, TState> Declare(TState state, StateNodeKind kind, int behaviourIndex)
        {
            var ordinal = EnumOrdinal.Of(state);

            if (_ordinalToDraft.ContainsKey(ordinal))
            {
                return Fail(MachineError.DuplicateState(ordinal));
            }

            var parent = _scopes.Count > 0 ? _scopes[^1] : ROOT;
            var index = _nodes.Count;

            _nodes.Add(new NodeDraft {
                Parent = parent,
                Ordinal = ordinal,
                Kind = kind,
                History = HistoryMode.None,
                InitialOrdinal = -1,
                BehaviourIndex = behaviourIndex,
            });

            _ordinalToDraft[ordinal] = index;
            _cursor = index;
            _pendingTransition = -1;
            return this;
        }

        private void PushScope()
        {
            if (_hasError == false)
            {
                _scopes.Add(_cursor);
            }
        }

        private MachineBuilder<TContext, TState> PopScope(StateNodeKind expected)
        {
            if (_scopes.Count == 0)
            {
                return Fail(MachineError.UnbalancedScope(NodeIndex.Of(_cursor), 0));
            }

            var node = _scopes[^1];

            if (_nodes[node].Kind != expected)
            {
                return Fail(MachineError.UnbalancedScope(NodeIndex.Of(node), _scopes.Count));
            }

            _scopes.RemoveAt(_scopes.Count - 1);
            _cursor = node;
            _pendingTransition = -1;
            return this;
        }

        private StateNodeKind CurrentScopeKind()
            => _scopes.Count > 0 ? _nodes[_scopes[^1]].Kind : StateNodeKind.Composite;

        private int AddBehaviour(StateBehaviour<TContext> behaviour)
        {
            if (behaviour == null)
            {
                return -1;
            }

            _behaviours.Add(behaviour);
            return _behaviours.Count - 1;
        }

        private int AddGuard(object guard, string expression)
        {
            _guards.Add(guard);
            _guardExpressions.Add(expression ?? string.Empty);
            return _guards.Count - 1;
        }

        private bool TryGetPending(out int index)
        {
            index = _pendingTransition;

            if (index < 0)
            {
                Fail(MachineError.NoPendingTransition());
                return false;
            }

            return true;
        }

        private MachineBuilder<TContext, TState> Fail(MachineError error)
        {
            if (_hasError == false)
            {
                _hasError = true;
                _error = error;
            }

            return this;
        }

        private struct NodeDraft
        {
            public int Parent;
            public int Ordinal;
            public StateNodeKind Kind;
            public HistoryMode History;
            public int InitialOrdinal;
            public int BehaviourIndex;
        }

        private struct TransitionDraft
        {
            public int SourceDraft;
            public int TargetOrdinal;
            public int GuardIndex;
            public int ActionIndex;
            public TriggerId Trigger;
            public float MinDuration;
            public int Priority;
            public TransitionFlags Flags;
            public int Order;
        }
    }
}
