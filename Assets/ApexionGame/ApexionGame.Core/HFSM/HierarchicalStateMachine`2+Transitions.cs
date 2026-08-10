using System;
using System.Runtime.CompilerServices;

namespace ApexionGame.HFSM
{
    partial class HierarchicalStateMachine<TContext, TState>
    {
        // ── active set ──────────────────────────────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsActive(int node)
            => (_activeMask[node >> 6] & (1UL << (node & 63))) != 0UL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetActive(int node)
            => _activeMask[node >> 6] |= 1UL << (node & 63);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearActive(int node)
            => _activeMask[node >> 6] &= ~(1UL << (node & 63));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsVisited(int node)
            => (_visitedMask[node >> 6] & (1UL << (node & 63))) != 0UL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetVisited(int node)
            => _visitedMask[node >> 6] |= 1UL << (node & 63);

        /// <summary>
        /// The deepest active leaf reached by following region 0 at every parallel node.
        /// </summary>
        private int PrimaryLeaf()
        {
            var node = ROOT;

            while (true)
            {
                var current = _nodes[node];

                if (current.Kind == StateNodeKind.Leaf)
                {
                    return node;
                }

                var child = current.Kind == StateNodeKind.Parallel
                    ? current.FirstChild
                    : _activeChild[node];

                if (child < 0 || IsActive(child) == false)
                {
                    return node;
                }

                node = child;
            }
        }

        /// <summary>
        /// Walks both nodes up to the depth they share, then up together. O(depth) with no search,
        /// because <see cref="StateNode.Depth"/> is baked.
        /// </summary>
        private int Lca(int a, int b)
        {
            while (_nodes[a].Depth > _nodes[b].Depth)
            {
                a = _nodes[a].Parent;
            }

            while (_nodes[b].Depth > _nodes[a].Depth)
            {
                b = _nodes[b].Parent;
            }

            while (a != b)
            {
                a = _nodes[a].Parent;
                b = _nodes[b].Parent;
            }

            return a;
        }

        /// <summary>
        /// True when going from <paramref name="from"/> to <paramref name="to"/> would mean
        /// crossing from one region of a parallel node into a sibling region, rather than passing
        /// through the parallel node's own gateway.
        /// </summary>
        /// <remarks>
        /// The build-time check in <c>MachineBuilder+Validate</c> covers a transition whose source
        /// is fixed at declaration time. An <c>AnyState</c> transition has no fixed source, so this
        /// is the runtime counterpart, evaluated against whichever leaf is actually being asked.
        /// </remarks>
        private bool CrossesParallelRegion(int from, int to)
        {
            if (from == to)
            {
                return false;
            }

            var lca = Lca(from, to);
            return _nodes[lca].Kind == StateNodeKind.Parallel && lca != from && lca != to;
        }

        /// <summary>
        /// The child of <paramref name="ancestor"/> that <paramref name="descendant"/> sits under.
        /// </summary>
        private int ChildTowards(int ancestor, int descendant)
        {
            var node = descendant;

            while (node >= 0 && _nodes[node].Parent != ancestor)
            {
                node = _nodes[node].Parent;
            }

            return node;
        }

        private int CollectActiveLeafNodes()
        {
            var count = 0;
            CollectActiveLeafNodes(ROOT, ref count);
            return count;
        }

        private void CollectActiveLeafNodes(int node, ref int count)
        {
            var current = _nodes[node];

            if (current.Kind == StateNodeKind.Leaf)
            {
                if (count < _leafScratch.Length)
                {
                    _leafScratch[count++] = node;
                }

                return;
            }

            if (current.Kind == StateNodeKind.Parallel)
            {
                for (var i = 0; i < current.ChildCount; i++)
                {
                    var child = current.FirstChild + i;

                    if (IsActive(child))
                    {
                        CollectActiveLeafNodes(child, ref count);
                    }
                }

                return;
            }

            var active = _activeChild[node];

            if (active >= 0 && IsActive(active))
            {
                CollectActiveLeafNodes(active, ref count);
                return;
            }

            // A container with nothing active beneath it is itself the tip of this branch.
            if (count < _leafScratch.Length)
            {
                _leafScratch[count++] = node;
            }
        }

        // ── planning ────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fills the exit and enter plans for a transition, without touching the machine.
        /// </summary>
        /// <remarks>
        /// Splitting plan from apply is what lets the synchronous and the asynchronous paths share
        /// one traversal: the async driver walks the same two lists, awaiting instead of calling.
        /// <para>
        /// History is recorded here, before anything exits — earlier than the contract requires, and
        /// therefore always early enough.
        /// </para>
        /// </remarks>
        private void PlanTransition(int source, int target)
        {
            _exitCount = 0;
            _enterCount = 0;

            int lca;

            if (source == target)
            {
                lca = _nodes[source].Parent;

                if (lca < 0)
                {
                    lca = ROOT;
                }
            }
            else
            {
                lca = Lca(source, target);
            }

            var lcaNode = _nodes[lca];

            if (lcaNode.Kind == StateNodeKind.Parallel)
            {
                if (source == lca)
                {
                    for (var i = lcaNode.ChildCount - 1; i >= 0; i--)
                    {
                        var child = lcaNode.FirstChild + i;

                        if (IsActive(child))
                        {
                            PlanExitNode(child);
                        }
                    }
                }
                else
                {
                    var region = ChildTowards(lca, source);

                    if (region >= 0 && IsActive(region))
                    {
                        PlanExitNode(region);
                    }
                }
            }
            else
            {
                var child = _activeChild[lca];

                if (child >= 0 && IsActive(child))
                {
                    PlanExitNode(child);
                }
            }

            if (target == lca)
            {
                PlanChildren(lca, -1, false);
            }
            else
            {
                PlanEnterNode(ChildTowards(lca, target), target, false);
            }
        }

        private void PlanExitNode(int node)
        {
            var current = _nodes[node];

            if (current.Kind == StateNodeKind.Parallel)
            {
                for (var i = current.ChildCount - 1; i >= 0; i--)
                {
                    var child = current.FirstChild + i;

                    if (IsActive(child))
                    {
                        PlanExitNode(child);
                    }
                }
            }
            else if (current.IsSingleChildContainer)
            {
                var child = _activeChild[node];
                _historyChild[node] = child;

                if (child >= 0 && IsActive(child))
                {
                    PlanExitNode(child);
                }
            }

            _exitPlan[_exitCount++] = node;
        }

        private void PlanEnterNode(int node, int target, bool inheritedDeep)
        {
            _enterPlan[_enterCount++] = node;

            if (_nodes[node].Kind == StateNodeKind.Leaf)
            {
                return;
            }

            PlanChildren(node, target, inheritedDeep);
        }

        private void PlanChildren(int node, int target, bool inheritedDeep)
        {
            var current = _nodes[node];
            var hasTarget = target >= 0 && target != node;
            var childDeep = inheritedDeep || current.History == HistoryMode.Deep;

            if (current.Kind == StateNodeKind.Parallel)
            {
                var onPath = hasTarget ? ChildTowards(node, target) : -1;

                for (var i = 0; i < current.ChildCount; i++)
                {
                    var child = current.FirstChild + i;
                    PlanEnterNode(child, child == onPath ? target : -1, childDeep);
                }

                return;
            }

            int next;

            if (hasTarget)
            {
                next = ChildTowards(node, target);
            }
            else
            {
                // A node consults its remembered child when it declares history of its own, or when
                // a Deep ancestor is restoring the whole path through it.
                var useHistory = inheritedDeep || current.History != HistoryMode.None;
                var remembered = _historyChild[node];
                next = useHistory && remembered >= 0 ? remembered : current.InitialChild;
            }

            if (next < 0)
            {
                return;
            }

            // Once this node fell back to history/initial rather than following a direct path to
            // `target`, `target` is no longer meaningfully "below" the child we just picked — it may
            // be unrelated to it entirely (an ancestor, or a node in a different branch). Passing it
            // forward regardless would make the child's OWN PlanChildren call wrongly believe it has
            // a specific descendant to route towards, compute ChildTowards == -1 for an unrelated
            // target, and stop descending — leaving this composite active with no leaf beneath it.
            PlanEnterNode(next, hasTarget ? target : -1, childDeep);
        }

        // ── applying ────────────────────────────────────────────────────────────────────────────

        private void ApplyExit()
        {
            for (var i = 0; i < _exitCount; i++)
            {
                var node = _exitPlan[i];
                InvokeExit(node);
                ClearActive(node);

                if (_nodes[node].IsSingleChildContainer)
                {
                    _activeChild[node] = -1;
                }
            }

            _exitCount = 0;
        }

        private void ApplyEnter(TransitionCause cause, int previousLeaf = -1)
        {
            for (var i = 0; i < _enterCount; i++)
            {
                var node = _enterPlan[i];
                var parent = _nodes[node].Parent;

                if (parent >= 0 && _nodes[parent].Kind != StateNodeKind.Parallel)
                {
                    _activeChild[parent] = node;
                }

                SetActive(node);
                _timeInNode[node] = 0f;
                InvokeEnter(node);
            }

            _enterCount = 0;
            RaiseStateChanged(previousLeaf, cause);
        }

        private void RaiseStateChanged(int previousLeaf, TransitionCause cause)
        {
            var leaf = PrimaryLeaf();

            if (leaf == previousLeaf)
            {
                return;
            }

            _previousLeaf = previousLeaf;

            if (StateChanged == null || previousLeaf < 0)
            {
                return;
            }

            StateChanged.Invoke(
                  _definition.StateOf(NodeIndex.Of(previousLeaf))
                , _definition.StateOf(NodeIndex.Of(leaf))
            );
        }

        private void ExitEverything()
        {
            _exitCount = 0;

            var child = _activeChild[ROOT];

            if (_nodes[ROOT].Kind == StateNodeKind.Parallel)
            {
                var root = _nodes[ROOT];

                for (var i = root.ChildCount - 1; i >= 0; i--)
                {
                    var region = root.FirstChild + i;

                    if (IsActive(region))
                    {
                        PlanExitNode(region);
                    }
                }
            }
            else if (child >= 0 && IsActive(child))
            {
                PlanExitNode(child);
            }

            ApplyExit();
            ClearActive(ROOT);
            _activeChild[ROOT] = -1;
        }

        // ── executing ───────────────────────────────────────────────────────────────────────────

        private void ExecuteTransition(int source, int target, int transitionIndex, TransitionCause cause)
        {
            var actionIndex = -1;
            var isInternal = false;

            if (transitionIndex >= 0)
            {
                var transition = _definition.Transitions[transitionIndex];
                actionIndex = transition.ActionIndex;
                isInternal = transition.IsInternal;
            }

            // Captured before anything runs: ApplyEnter resets a re-entered node's timer, so
            // reading it afterwards would report 0 for a self-transition.
            var timeInSource = _timeInNode[source];

            if (isInternal)
            {
                InvokeAction(actionIndex);
                RecordTransition(source, source, cause, transitionIndex, timeInSource);
                return;
            }

            var previousLeaf = PrimaryLeaf();
            PlanTransition(source, target);

            if (TryStartAsyncTransition(
                  source, target, actionIndex, transitionIndex, cause, previousLeaf, timeInSource
            ))
            {
                return;
            }

            ApplyExit();
            InvokeAction(actionIndex);
            ApplyEnter(cause, previousLeaf);
            RecordTransition(source, target, cause, transitionIndex, timeInSource);
        }

        // ── resolving ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gives every active region an independent chance to resolve and execute one transition
        /// this tick.
        /// </summary>
        /// <remarks>
        /// With no parallel nodes there is exactly one region (the machine's single leaf), so this
        /// reduces to the single-region case. With parallel regions, each active leaf gets its own
        /// turn — a queued trigger that matches its path wins over a polled guard, matching the
        /// single-region tie-break — and <see cref="IsActive"/> is re-checked before every turn,
        /// since an earlier region's transition can deactivate a later one in the same snapshot
        /// (an <c>AnyState</c> that exits the whole parallel node deactivates every sibling region).
        /// <para>
        /// Not deduplicated across regions by a visited set: a transition declared on a node shared
        /// by multiple regions (an ancestor above the parallel node) could in principle be evaluated
        /// — and, if it is a non-deactivating internal transition, fire — once per region that
        /// reaches it before the shared node is out of the picture. This is a known, narrow
        /// limitation; the documented, tested shapes (each region's own transitions, and an
        /// <c>AnyState</c> that exits everything) are unaffected because firing the shared
        /// transition once already deactivates every other region's leaf before its turn comes up.
        /// </para>
        /// </remarks>
        private void ResolveTransitions()
        {
            if (_requestedTarget >= 0)
            {
                var target = _requestedTarget;
                _requestedTarget = -1;
                ExecuteTransition(PrimaryLeaf(), target, -1, TransitionCause.Request);
                return;
            }

            var leafCount = CollectActiveLeafNodes();

            for (var l = 0; l < leafCount; l++)
            {
                var leaf = _leafScratch[l];

                if (IsActive(leaf) == false)
                {
                    continue;
                }

                if (_pendingTriggerCount > 0 && TryConsumeTriggerFor(leaf, out var triggerSource, out var triggerIndex))
                {
                    ExecuteTransition(
                          triggerSource
                        , _definition.Transitions[triggerIndex].Target
                        , triggerIndex
                        , TransitionCause.Trigger
                    );
                }
                else if (TryMatchWalkUp(leaf, false, TriggerId.None, out var guardSource, out var guardIndex))
                {
                    var transition = _definition.Transitions[guardIndex];
                    ExecuteTransition(guardSource, transition.Target, guardIndex, transition.Cause);
                }
                else
                {
                    continue;
                }

                if (_phase != MachinePhase.Idle)
                {
                    // An async chain started. The remaining regions in this snapshot have not had
                    // their turn, so their triggers must stay queued for a future tick rather than
                    // being declared unmatched below.
                    return;
                }
            }

            DropUnmatchedTriggers();
        }

        /// <summary>
        /// Scans the queue in order for the first trigger whose walk-up from <paramref name="leaf"/>
        /// finds a match, removing exactly that one entry and leaving the rest queued.
        /// </summary>
        private bool TryConsumeTriggerFor(int leaf, out int source, out int index)
        {
            for (var t = 0; t < _pendingTriggerCount; t++)
            {
                if (TryMatchWalkUp(leaf, true, _pendingTriggers[t], out source, out index))
                {
                    RemoveTriggerAt(t);
                    return true;
                }
            }

            source = -1;
            index = -1;
            return false;
        }

        /// <summary>
        /// Every trigger still queued after every region has had its turn matched nothing this
        /// tick. Dropping rather than retaining it is deliberate — see
        /// <see cref="MachineError"/> §DEC-007 in the design docs: keeping an unmatched trigger
        /// queued means it resurfaces minutes later the moment some region happens to reach a state
        /// that handles it, and that bug is very hard to trace back to its cause. The drop is
        /// recorded instead.
        /// </summary>
        private void DropUnmatchedTriggers()
        {
            for (var i = 0; i < _pendingTriggerCount; i++)
            {
                RecordUnmatchedTrigger(_pendingTriggers[i]);
            }

            _pendingTriggerCount = 0;
        }

        private void RemoveTriggerAt(int i)
        {
            for (var j = i; j < _pendingTriggerCount - 1; j++)
            {
                _pendingTriggers[j] = _pendingTriggers[j + 1];
            }

            _pendingTriggerCount--;
        }

        /// <summary>
        /// Walks from <paramref name="leaf"/> up to the root, checking the <c>AnyState</c> slice
        /// first, then each node's own outgoing transitions, returning the first eligible match.
        /// </summary>
        private bool TryMatchWalkUp(int leaf, bool triggered, TriggerId trigger, out int source, out int index)
        {
            if (TryMatchSlice(0, _definition.AnyStateCount, leaf, leaf, trigger, triggered, out index))
            {
                source = leaf;
                return true;
            }

            for (var node = leaf; node >= 0; node = _nodes[node].Parent)
            {
                var current = _nodes[node];

                if (current.TransitionCount == 0)
                {
                    continue;
                }

                if (TryMatchSlice(
                      current.TransitionStart
                    , current.TransitionCount
                    , node
                    , leaf
                    , trigger
                    , triggered
                    , out index
                ))
                {
                    source = node;
                    return true;
                }
            }

            source = -1;
            index = -1;
            return false;
        }

        private bool TryMatchSlice(
              int start
            , int count
            , int sourceNode
            , int leaf
            , TriggerId trigger
            , bool triggered
            , out int index
        )
        {
            var transitions = _definition.Transitions;
            var end = start + count;

            for (var i = start; i < end; i++)
            {
                var transition = transitions[i];

                if (triggered)
                {
                    if (transition.IsTriggered == false || transition.Trigger != trigger)
                    {
                        continue;
                    }
                }
                else if (transition.IsTriggered)
                {
                    continue;
                }

                if (_timeInNode[sourceNode] < transition.MinDuration)
                {
                    continue;
                }

                // An AnyState transition has no fixed source, so build-time validation cannot know
                // which leaf will be evaluating it. A regular transition's source is fixed and was
                // already checked once at build time, so this is a no-op for it — but for AnyState,
                // this is the only place that can catch "leaf in region A, target inside region B":
                // planning that transition would enter an already-active sibling region without
                // exiting it first.
                if (transition.IsAnyState && CrossesParallelRegion(sourceNode, transition.Target))
                {
                    continue;
                }

                if (transition.HasGuard && EvaluateGuard(transition, sourceNode, leaf) == false)
                {
                    continue;
                }

                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        private bool EvaluateGuard(in Transition transition, int sourceNode, int leaf)
        {
            var guard = _definition.Guards[transition.GuardIndex];

            if ((transition.Flags & TransitionFlags.GuardTakesInfo) != 0)
            {
                var info = new GuardInfo(
                      transition.IsAnyState ? NodeIndex.Invalid : NodeIndex.Of(sourceNode)
                    , NodeIndex.Of(leaf)
                    , NodeIndex.Of(_previousLeaf)
                    , _timeInNode[sourceNode]
                    , _timeInMachine
                );

                return ((GuardWithInfo<TContext>)guard).Invoke(_context, info);
            }

            return ((Guard<TContext>)guard).Invoke(_context);
        }

        // ── updating ────────────────────────────────────────────────────────────────────────────

        private void UpdateNode(int node, float deltaTime)
        {
            InvokeUpdate(node, deltaTime);

            var current = _nodes[node];

            if (current.Kind == StateNodeKind.Leaf)
            {
                return;
            }

            if (current.Kind == StateNodeKind.Parallel)
            {
                for (var i = 0; i < current.ChildCount; i++)
                {
                    var child = current.FirstChild + i;

                    if (IsActive(child))
                    {
                        UpdateNode(child, deltaTime);
                    }
                }

                return;
            }

            var active = _activeChild[node];

            if (active >= 0 && IsActive(active))
            {
                UpdateNode(active, deltaTime);
            }
        }

        // ── behaviour dispatch ──────────────────────────────────────────────────────────────────

        private StateInfo InfoFor(int node)
            => new(
                  NodeIndex.Of(node)
                , NodeIndex.Of(_previousLeaf)
                , _timeInNode[node]
                , _timeInMachine
                , this
            );

        private void InvokeEnter(int node)
        {
            var current = _nodes[node];

            if (current.BehaviourIndex < 0)
            {
                return;
            }

            _definition.Behaviours[current.BehaviourIndex]
                .EnterCore(_context, _stateData, current.StateDataOffset, InfoFor(node));
        }

        private void InvokeUpdate(int node, float deltaTime)
        {
            var current = _nodes[node];

            if (current.BehaviourIndex < 0)
            {
                return;
            }

            _definition.Behaviours[current.BehaviourIndex]
                .UpdateCore(_context, _stateData, current.StateDataOffset, InfoFor(node), deltaTime);
        }

        private void InvokeExit(int node)
        {
            var current = _nodes[node];

            if (current.BehaviourIndex < 0)
            {
                return;
            }

            _definition.Behaviours[current.BehaviourIndex]
                .ExitCore(_context, _stateData, current.StateDataOffset, InfoFor(node));
        }

        private void InvokeAction(int actionIndex)
        {
            if (actionIndex >= 0)
            {
                _definition.Actions[actionIndex].Invoke(_context);
            }
        }
    }
}
