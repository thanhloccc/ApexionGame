using System;
using System.Runtime.CompilerServices;
using ApexionGame.HFSM.Internals;
using EncosyTower.Common;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// One running machine: a context, an active configuration, and the timers and history that go
    /// with them.
    /// </summary>
    /// <remarks>
    /// Cheap on purpose. Everything about the machine's <i>shape</i> lives in the shared
    /// <see cref="MachineDefinition{TContext, TState}"/>; an instance is a handful of small arrays
    /// plus the caller's blackboard.
    /// </remarks>
    public sealed partial class HierarchicalStateMachine<TContext, TState>
        : IMachineControl
        , IMachineTickable
        , IDisposable
        where TContext : class
        where TState : unmanaged, Enum
    {
        private const int ROOT = MachineBuilder<TContext, TState>.ROOT;
        private const int TRIGGER_CAPACITY = 16;

        private readonly MachineDefinition<TContext, TState> _definition;
        private readonly StateNode[] _nodes;
        private readonly int[] _activeChild;
        private readonly int[] _historyChild;
        private readonly float[] _timeInNode;
        private readonly ulong[] _activeMask;
        private readonly ulong[] _visitedMask;
        private readonly byte[] _stateData;
        private readonly TriggerId[] _pendingTriggers = new TriggerId[TRIGGER_CAPACITY];

        // Scratch is per-instance fixed arrays rather than pooled lists: an async transition holds
        // its plan across ticks, so a rented buffer would have to survive an await. Sized by node
        // count, which bounds every chain the machine can produce.
        private readonly int[] _exitPlan;
        private readonly int[] _enterPlan;
        private readonly int[] _leafScratch;

        private int _pendingTriggerCount;
        private int _exitCount;
        private int _enterCount;

        private TContext _context;
        private MachineRunner _runner;
        private string _debugName;
        private MachinePhase _phase = MachinePhase.Stopped;
        private AsyncPolicy _asyncPolicy;
        private float _timeInMachine;
        private int _previousLeaf = -1;
        private int _requestedTarget = -1;
        private int _instanceId;
        private int _frame;

        // Bumped on every Initialize/Dispose. An async transition captures this before its first
        // await and re-checks it after every later one; a mismatch means this instance has since
        // been disposed and possibly handed to a different context by the pool, and the stale
        // continuation must touch nothing and return immediately.
        private int _generation;

        internal HierarchicalStateMachine(MachineDefinition<TContext, TState> definition)
        {
            _definition = definition;
            _nodes = definition.Nodes;

            var count = definition.NodeCount;
            _activeChild = new int[count];
            _historyChild = new int[count];
            _timeInNode = new float[count];
            _activeMask = new ulong[(count + 63) / 64];
            _visitedMask = new ulong[(count + 63) / 64];
            _stateData = new byte[definition.StateDataSize];
            _exitPlan = new int[count];
            _enterPlan = new int[count];
            _leafScratch = new int[count];
        }

        /// <summary>
        /// Opens a declaration for a machine over this context and state enum.
        /// </summary>
        /// <remarks>
        /// A static member of the machine class itself; there is no separate factory type.
        /// </remarks>
        public static MachineBuilder<TContext, TState> Define(string name)
            => new(name);

        /// <summary>
        /// Raised when the primary active leaf changes. Not raised for entering or leaving
        /// composites.
        /// </summary>
        public event Action<TState, TState> StateChanged;

        public MachineDefinition<TContext, TState> Definition => _definition;

        public TContext Context => _context;

        public MachinePhase Phase => _phase;

        public bool IsRunning => _phase != MachinePhase.Stopped;

        public string DebugName => _debugName;

        public float TimeInMachine => _timeInMachine;

        /// <summary>
        /// Seconds the primary active leaf has been active.
        /// </summary>
        public float TimeInState
        {
            get
            {
                var leaf = PrimaryLeaf();
                return leaf >= 0 ? _timeInNode[leaf] : 0f;
            }
        }

        /// <summary>
        /// The deepest active leaf of the first region.
        /// </summary>
        /// <remarks>
        /// With parallel regions there is more than one active leaf and this returns region 0's.
        /// Use <see cref="GetActiveStates"/> when that matters.
        /// </remarks>
        public TState CurrentState => _definition.StateOf(CurrentNode);

        /// <inheritdoc cref="CurrentState"/>
        public NodeIndex CurrentNode
        {
            get
            {
                var leaf = PrimaryLeaf();
                return leaf >= 0 ? NodeIndex.Of(leaf) : NodeIndex.Invalid;
            }
        }

        // ── lifecycle ───────────────────────────────────────────────────────────────────────────

        internal void Initialize(TContext context, in MachineOptions options, int instanceId)
        {
            _generation++;
            _context = context;
            _instanceId = instanceId;
            _asyncPolicy = options.AsyncPolicy;
            _debugName = options.DebugName ?? $"{_definition.Name} #{instanceId:D4}";

            ClearState();

            if (options.TickMode == TickMode.Runner)
            {
                _runner = options.Runner ?? MachineRunner.Default;
                _runner.Register(this);
            }
            else
            {
                _runner = null;
            }

            RegisterForDebugging();

            if (options.DeferStart == false)
            {
                Start();
            }
        }

        /// <summary>
        /// Enters the initial configuration. Called for you unless
        /// <see cref="MachineOptions.DeferStart"/> was set.
        /// </summary>
        public void Start()
        {
            if (_phase != MachinePhase.Stopped)
            {
                return;
            }

            _phase = MachinePhase.Idle;
            _previousLeaf = -1;

            _exitCount = 0;
            _enterCount = 0;
            PlanEnterNode(ROOT, -1, false);
            ApplyEnter(TransitionCause.Initial);
        }

        /// <summary>
        /// Returns to the initial configuration, clearing history, timers and state data.
        /// </summary>
        /// <remarks>
        /// Exits the active configuration first, so behaviours get their <c>OnExit</c>. State data
        /// always starts zeroed after this.
        /// </remarks>
        public void Reset()
        {
            // Same hazard as Dispose: a suspended async transition must not resume into a
            // configuration that was reset out from under it.
            _generation++;
            CancelInFlight();

            if (_phase != MachinePhase.Stopped)
            {
                ExitEverything();
            }

            ClearState();
            _phase = MachinePhase.Stopped;
            Start();
        }

        public void Dispose()
        {
            if (_context == null)
            {
                return;
            }

            // Must happen before CancelInFlight: an in-flight coroutine checks this on its next
            // resumption regardless of the cancellation token, since cancellation only asks it to
            // stop — it does not guarantee it has stopped before this method returns.
            _generation++;
            CancelInFlight();

            if (_phase != MachinePhase.Stopped)
            {
                ExitEverything();
            }

            UnregisterFromDebugging();
            _runner?.Unregister(this);
            _runner = null;

            StateChanged = null;
            _phase = MachinePhase.Stopped;
            _context = null;
            _debugName = null;

            ClearState();
            _definition.Return(this);
        }

        private void ClearState()
        {
            Array.Fill(_activeChild, -1);
            Array.Fill(_historyChild, -1);
            Array.Clear(_timeInNode, 0, _timeInNode.Length);
            Array.Clear(_activeMask, 0, _activeMask.Length);
            Array.Clear(_stateData, 0, _stateData.Length);

            _pendingTriggerCount = 0;
            _exitCount = 0;
            _enterCount = 0;
            _timeInMachine = 0f;
            _previousLeaf = -1;
            _requestedTarget = -1;
            ClearLog();
        }

        // ── tick ────────────────────────────────────────────────────────────────────────────────

        public void Tick(float deltaTime)
        {
            if (_phase == MachinePhase.Stopped)
            {
                return;
            }

            _timeInMachine += deltaTime;
            _frame++;

            if (_phase != MachinePhase.Idle)
            {
                // An async chain is driving itself; only the machine clock advances.
                return;
            }

            for (var i = 0; i < _timeInNode.Length; i++)
            {
                if (IsActive(i))
                {
                    _timeInNode[i] += deltaTime;
                }
            }

            ResolveTransitions();

            if (_phase == MachinePhase.Idle)
            {
                UpdateNode(ROOT, deltaTime);
            }
        }

        // ── triggers and requests ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Queues a trigger. Never applied synchronously — it is drained at a defined point in the
        /// tick, so firing from inside a behaviour cannot reorder the machine underneath it.
        /// </summary>
        public void Fire<TTrigger>(TTrigger trigger)
            where TTrigger : unmanaged, Enum
        {
            var result = FireOrError(trigger);

            if (result.IsError)
            {
                LogWarning(result.GetErrorOrDefault());
            }
        }

        /// <inheritdoc cref="Fire{TTrigger}(TTrigger)"/>
        public Result<TriggerId, MachineError> FireOrError<TTrigger>(TTrigger trigger)
            where TTrigger : unmanaged, Enum
        {
            var id = TriggerId.Of(trigger);

            if (_phase == MachinePhase.Stopped)
            {
                return Result<TriggerId, MachineError>.Err(
                    MachineError.MachineNotRunning().Prefix(nameof(Fire))
                );
            }

            if (_pendingTriggerCount >= TRIGGER_CAPACITY)
            {
                return Result<TriggerId, MachineError>.Err(
                    MachineError.TriggerQueueFull(id, TRIGGER_CAPACITY).Prefix(nameof(Fire))
                );
            }

            _pendingTriggers[_pendingTriggerCount++] = id;
            return id;
        }

        /// <summary>
        /// Asks for a transition without a guard or a trigger.
        /// </summary>
        /// <remarks>
        /// Applied at the start of the next tick rather than immediately, so calling it from inside
        /// <c>OnUpdate</c> cannot mutate the configuration the update walk is standing on.
        /// </remarks>
        public Result<NodeIndex, MachineError> RequestTransitionOrError(TState target)
            => RequestTransitionOrError(_definition.IndexOf(target));

        /// <inheritdoc cref="RequestTransitionOrError(TState)"/>
        public Result<NodeIndex, MachineError> RequestTransitionOrError(NodeIndex target)
        {
            if (_phase == MachinePhase.Stopped)
            {
                return Result<NodeIndex, MachineError>.Err(
                    MachineError.MachineNotRunning().Prefix(nameof(RequestTransitionOrError))
                );
            }

            if (target.IsWithin(_nodes.Length) == false)
            {
                return Result<NodeIndex, MachineError>.Err(
                    MachineError.UnknownState(target.value).Prefix(nameof(RequestTransitionOrError))
                );
            }

            if (_phase != MachinePhase.Idle)
            {
                if (_asyncPolicy == AsyncPolicy.Ignore)
                {
                    return Result<NodeIndex, MachineError>.Err(
                        MachineError.TransitionInFlight(CurrentNode, target)
                            .Prefix(nameof(RequestTransitionOrError))
                    );
                }

                // Queue policy parks the target behind the running chain and reports success;
                // CancelAndReplace tears the chain down and falls through to run it now.
                if (AcceptWhileInFlight(target.value) == false)
                {
                    return target;
                }
            }

            _requestedTarget = target.value;
            return target;
        }

        // ── queries ─────────────────────────────────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive(TState state)
        {
            var node = _definition.IndexOf(state);
            return node.IsWithin(_nodes.Length) && IsActive(node.value);
        }

        /// <summary>
        /// Fills <paramref name="destination"/> with every active leaf, in region order.
        /// </summary>
        /// <returns>How many were written.</returns>
        public int GetActiveStates(Span<TState> destination)
        {
            var written = 0;
            CollectActiveLeaves(ROOT, destination, ref written);
            return written;
        }

        private void CollectActiveLeaves(int node, Span<TState> destination, ref int written)
        {
            var current = _nodes[node];

            if (current.Kind == StateNodeKind.Leaf)
            {
                if (written < destination.Length)
                {
                    destination[written] = _definition.StateOf(NodeIndex.Of(node));
                }

                written++;
                return;
            }

            if (current.Kind == StateNodeKind.Parallel)
            {
                for (var i = 0; i < current.ChildCount; i++)
                {
                    var child = current.FirstChild + i;

                    if (IsActive(child))
                    {
                        CollectActiveLeaves(child, destination, ref written);
                    }
                }

                return;
            }

            var active = _activeChild[node];

            if (active >= 0 && IsActive(active))
            {
                CollectActiveLeaves(active, destination, ref written);
            }
        }

        // IMachineControl, IMachineTickable: every member above already matches an interface
        // member's signature, so both interfaces are satisfied implicitly. No explicit
        // implementation here — one would collide with the public method of the same signature.

        public override string ToString()
            => _debugName ?? _definition.Name;
    }
}
