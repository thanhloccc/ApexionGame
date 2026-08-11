using System;
using System.Collections.Generic;
using ApexionGame.HFSM.Internals;
using EncosyTower.Common;

namespace ApexionGame.HFSM
{
    partial class MachineBuilder<TContext, TState>
    {
        /// <summary>
        /// Bakes the declaration into an immutable definition, or reports the first thing wrong
        /// with it.
        /// </summary>
        /// <remarks>
        /// Every validation error listed on <see cref="MachineError"/> is produced here, and every
        /// one of them has a test that feeds it a real malformed declaration.
        /// </remarks>
        public Result<MachineDefinition<TContext, TState>, MachineError> BuildOrError()
        {
            if (_hasError)
            {
                return Result<MachineDefinition<TContext, TState>, MachineError>.Err(_error);
            }

            if (_scopes.Count > 0)
            {
                return Err(MachineError.UnbalancedScope(NodeIndex.Of(_scopes[^1]), _scopes.Count));
            }

            if (_nodes.Count <= 1)
            {
                return Err(MachineError.EmptyMachine());
            }

            var draftCount = _nodes.Count;

            // Children, in declaration order. Needed before anything else can be resolved.
            var children = new List<int>[draftCount];

            for (var i = 0; i < draftCount; i++)
            {
                children[i] = new List<int>();
            }

            for (var i = 1; i < draftCount; i++)
            {
                children[_nodes[i].Parent].Add(i);
            }

            // A container with no children can never reach a leaf.
            for (var i = 0; i < draftCount; i++)
            {
                if (_nodes[i].Kind != StateNodeKind.Leaf && children[i].Count == 0)
                {
                    return Err(MachineError.EmptyComposite(NodeIndex.Of(i)));
                }
            }

            // Lay nodes out breadth-first so every node's children are contiguous, which is what
            // lets StateNode address them as (FirstChild, ChildCount).
            var finalOf = new int[draftCount];
            var draftOf = new List<int>(draftCount);

            Array.Fill(finalOf, -1);
            finalOf[ROOT] = 0;
            draftOf.Add(ROOT);

            var queue = new Queue<int>();
            queue.Enqueue(ROOT);

            while (queue.Count > 0)
            {
                var draft = queue.Dequeue();

                foreach (var child in children[draft])
                {
                    finalOf[child] = draftOf.Count;
                    draftOf.Add(child);
                    queue.Enqueue(child);
                }
            }

            var nodeCount = draftOf.Count;
            var parents = new int[nodeCount];
            var depths = new ushort[nodeCount];

            for (var i = 0; i < nodeCount; i++)
            {
                var draft = _nodes[draftOf[i]];
                parents[i] = draft.Parent < 0 ? -1 : finalOf[draft.Parent];
                depths[i] = i == 0 ? (ushort)0 : (ushort)(depths[parents[i]] + 1);
            }

            // Initial children. A container without an explicit one falls back to its first child;
            // a parallel node has none, because all of its regions are active at once.
            var initialChildren = new int[nodeCount];

            for (var i = 0; i < nodeCount; i++)
            {
                var draftIndex = draftOf[i];
                var draft = _nodes[draftIndex];

                if (draft.Kind == StateNodeKind.Leaf || draft.Kind == StateNodeKind.Parallel)
                {
                    initialChildren[i] = -1;
                    continue;
                }

                if (draft.InitialOrdinal < 0)
                {
                    initialChildren[i] = finalOf[children[draftIndex][0]];
                    continue;
                }

                if (_ordinalToDraft.TryGetValue(draft.InitialOrdinal, out var initialDraft) == false)
                {
                    return Err(MachineError.UnknownState(draft.InitialOrdinal));
                }

                if (_nodes[initialDraft].Parent != draftIndex)
                {
                    return Err(MachineError.InitialChildNotAChild(
                          NodeIndex.Of(i)
                        , NodeIndex.Of(finalOf[initialDraft])
                    ));
                }

                initialChildren[i] = finalOf[initialDraft];
            }

            // Transitions: resolve endpoints, then reject the one topology that has no defined
            // meaning — a transition between two regions of the same parallel node.
            var resolved = new List<(Transition transition, int order)>(_transitions.Count);

            foreach (var draft in _transitions)
            {
                if (_ordinalToDraft.TryGetValue(draft.TargetOrdinal, out var targetDraft) == false)
                {
                    return Err(MachineError.UnknownState(draft.TargetOrdinal));
                }

                var source = draft.SourceDraft < 0 ? -1 : finalOf[draft.SourceDraft];
                var target = finalOf[targetDraft];

                if (source >= 0 && source != target)
                {
                    var lca = LowestCommonAncestor(parents, depths, source, target);

                    if (_nodes[draftOf[lca]].Kind == StateNodeKind.Parallel
                        && lca != source
                        && lca != target
                    )
                    {
                        return Err(MachineError.TransitionCrossesParallelRegion(
                              NodeIndex.Of(source)
                            , NodeIndex.Of(target)
                        ));
                    }
                }

                resolved.Add((new Transition(
                      source
                    , target
                    , draft.GuardIndex
                    , draft.ActionIndex
                    , draft.Trigger
                    , draft.MinDuration
                    , draft.Priority
                    , draft.Flags
                ), draft.Order));
            }

            // Any-state first, then grouped by source, then priority descending, then declaration
            // order. Sorting here is what makes a node's outgoing transitions a contiguous slice
            // that is already in evaluation order.
            resolved.Sort(static (x, y) => {
                var bySource = x.transition.Source.CompareTo(y.transition.Source);

                if (bySource != 0)
                {
                    return bySource;
                }

                var byPriority = y.transition.Priority.CompareTo(x.transition.Priority);
                return byPriority != 0 ? byPriority : x.order.CompareTo(y.order);
            });

            var transitions = new Transition[resolved.Count];
            var transitionStarts = new int[nodeCount];
            var transitionCounts = new int[nodeCount];
            var anyStateCount = 0;

            for (var i = 0; i < resolved.Count; i++)
            {
                var transition = resolved[i].transition;
                transitions[i] = transition;

                if (transition.Source < 0)
                {
                    anyStateCount++;
                    continue;
                }

                if (transitionCounts[transition.Source] == 0)
                {
                    transitionStarts[transition.Source] = i;
                }

                transitionCounts[transition.Source]++;
            }

            // State-data slots, aligned so a `ref` into the blob is never misaligned.
            var behaviourIndices = new int[nodeCount];
            var dataOffsets = new int[nodeCount];
            var dataSize = 0;

            for (var i = 0; i < nodeCount; i++)
            {
                var behaviourIndex = _nodes[draftOf[i]].BehaviourIndex;
                behaviourIndices[i] = behaviourIndex;
                dataOffsets[i] = -1;

                if (behaviourIndex < 0)
                {
                    continue;
                }

                var behaviour = _behaviours[behaviourIndex];
                var size = behaviour.DataSize;

                if (size <= 0)
                {
                    continue;
                }

                var align = Math.Max(1, behaviour.DataAlign);
                dataSize = (dataSize + align - 1) / align * align;
                dataOffsets[i] = dataSize;
                dataSize += size;
            }

            var nodes = new StateNode[nodeCount];

            for (var i = 0; i < nodeCount; i++)
            {
                var draftIndex = draftOf[i];
                var draft = _nodes[draftIndex];
                var childList = children[draftIndex];

                nodes[i] = new StateNode(
                      parents[i]
                    , childList.Count > 0 ? finalOf[childList[0]] : -1
                    , childList.Count
                    , initialChildren[i]
                    , transitionStarts[i]
                    , transitionCounts[i]
                    , dataOffsets[i]
                    , behaviourIndices[i]
                    , depths[i]
                    , draft.Kind
                    , draft.History
                );
            }

            // Ordinal lookup, dense so IndexOf is an array read.
            var maxOrdinal = 0;

            foreach (var pair in _ordinalToDraft)
            {
                maxOrdinal = Math.Max(maxOrdinal, pair.Key);
            }

            var ordinalToNode = new int[maxOrdinal + 1];
            Array.Fill(ordinalToNode, -1);

            var nodeToState = new TState[nodeCount];

            for (var i = 0; i < nodeCount; i++)
            {
                var ordinal = _nodes[draftOf[i]].Ordinal;

                if (ordinal < 0)
                {
                    continue;
                }

                ordinalToNode[ordinal] = i;
                nodeToState[i] = EnumOrdinal.From<TState>(ordinal);
            }

            var definition = new MachineDefinition<TContext, TState>(
                  _name
                , nodes
                , transitions
                , anyStateCount
                , _behaviours.ToArray()
                , _guards.ToArray()
                , _guardExpressions.ToArray()
                , _actions.ToArray()
                , ordinalToNode
                , nodeToState
                , dataSize
            );

            return definition;
        }

        /// <summary>
        /// Bakes the declaration, throwing if it is malformed. For a definition built into a
        /// <see langword="static readonly"/> field, where a failure is a programming error rather
        /// than something to handle.
        /// </summary>
        public MachineDefinition<TContext, TState> BuildOrThrow()
        {
            var result = BuildOrError();

            if (result.TryGetValue(out var definition))
            {
                return definition;
            }

            throw new InvalidOperationException(result.GetErrorOrDefault().ToString());
        }

        private static Result<MachineDefinition<TContext, TState>, MachineError> Err(MachineError error)
            => Result<MachineDefinition<TContext, TState>, MachineError>.Err(error);

        private static int LowestCommonAncestor(int[] parents, ushort[] depths, int a, int b)
        {
            while (depths[a] > depths[b])
            {
                a = parents[a];
            }

            while (depths[b] > depths[a])
            {
                b = parents[b];
            }

            while (a != b)
            {
                a = parents[a];
                b = parents[b];
            }

            return a;
        }
    }
}
