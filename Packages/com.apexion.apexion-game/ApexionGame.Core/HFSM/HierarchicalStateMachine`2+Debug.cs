using System.Collections.Generic;
using System.Diagnostics;
using ApexionGame.HFSM.Debugging;
using EncosyTower.Logging;

namespace ApexionGame.HFSM
{
    partial class HierarchicalStateMachine<TContext, TState> : IMachineDebug
    {
        private const int LOG_CAPACITY = 32;

        // Allocated only under the debug define, by RegisterForDebugging — never on the tick path
        // of a release build.
        private TransitionLogEntry[] _log;
        private int _logHead;
        private int _logCount;

        // ── IMachineDebug ───────────────────────────────────────────────────────────────────────

        string IMachineDebug.Name => _debugName;

        string IMachineDebug.DefinitionName => _definition.Name;

        bool IMachineDebug.IsAlive => _context != null;

        int IMachineDebug.NodeCount => _nodes.Length;

        void IMachineDebug.GetActiveNodes(List<NodeIndex> result)
        {
            // Node indices are baked breadth-first (every parent precedes its children), so an
            // ascending scan already visits outermost-first with no recursion needed.
            for (var i = 0; i < _nodes.Length; i++)
            {
                if (IsActive(i))
                {
                    result.Add(NodeIndex.Of(i));
                }
            }
        }

        void IMachineDebug.GetNodes(List<NodeDebugInfo> result)
        {
            for (var i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];

                result.Add(new NodeDebugInfo(
                      NodeIndex.Of(i)
                    , node.Parent >= 0 ? NodeIndex.Of(node.Parent) : NodeIndex.Invalid
                    , node.Kind
                    , node.History
                    , node.Depth
                    , IsActive(i)
                    , _historyChild[i] >= 0 ? NodeIndex.Of(_historyChild[i]) : NodeIndex.Invalid
                    , _timeInNode[i]
                ));
            }
        }

        void IMachineDebug.GetLog(List<TransitionLogEntry> result)
        {
            if (_log == null)
            {
                return;
            }

            var start = (_logHead - _logCount + _log.Length) % _log.Length;

            for (var i = 0; i < _logCount; i++)
            {
                result.Add(_log[(start + i) % _log.Length]);
            }
        }

        void IMachineDebug.GetTransitions(List<TransitionDebugInfo> result)
        {
            var transitions = _definition.Transitions;

            for (var i = 0; i < transitions.Length; i++)
            {
                var transition = transitions[i];

                result.Add(new TransitionDebugInfo(
                      transition.IsAnyState ? NodeIndex.Invalid : NodeIndex.Of(transition.Source)
                    , NodeIndex.Of(transition.Target)
                    , transition.IsTriggered
                    , (transition.Flags & TransitionFlags.Timed) != 0
                ));
            }
        }

        void IMachineDebug.GetOutgoingGuards(NodeIndex leaf, List<GuardDebugInfo> result)
        {
            if (leaf.IsWithin(_nodes.Length) == false)
            {
                return;
            }

            var leafNode = leaf.value;
            var wonAlready = false;

            AppendGuardSlice(0, _definition.AnyStateCount, leafNode, leafNode, result, ref wonAlready);

            for (var node = leafNode; node >= 0; node = _nodes[node].Parent)
            {
                var current = _nodes[node];

                if (current.TransitionCount > 0)
                {
                    AppendGuardSlice(
                          current.TransitionStart, current.TransitionCount, node, leafNode, result, ref wonAlready
                    );
                }
            }
        }

        /// <summary>
        /// The read-only counterpart of <see cref="TryMatchSlice"/>: reports every transition in the
        /// slice instead of stopping at the first match, so the guard inspector can show the ones
        /// that lost too.
        /// </summary>
        private void AppendGuardSlice(
              int start
            , int count
            , int sourceNode
            , int leaf
            , List<GuardDebugInfo> result
            , ref bool wonAlready
        )
        {
            var transitions = _definition.Transitions;
            var end = start + count;

            for (var i = start; i < end; i++)
            {
                var transition = transitions[i];

                if (transition.IsAnyState && CrossesParallelRegion(sourceNode, transition.Target))
                {
                    continue;
                }

                var source = transition.IsAnyState ? NodeIndex.Invalid : NodeIndex.Of(sourceNode);
                var target = NodeIndex.Of(transition.Target);

                if (transition.IsTriggered)
                {
                    result.Add(new GuardDebugInfo(
                          source, target, GuardMarker.Eligible, $"on {transition.Trigger.ToDisplayName()}", null, 0f
                    ));
                    continue;
                }

                if (_timeInNode[sourceNode] < transition.MinDuration)
                {
                    var remaining = transition.MinDuration - _timeInNode[sourceNode];
                    result.Add(new GuardDebugInfo(
                          source, target, GuardMarker.BlockedByMinDuration, ConditionTextOf(transition), null, remaining
                    ));
                    continue;
                }

                if (wonAlready)
                {
                    result.Add(new GuardDebugInfo(
                          source, target, GuardMarker.NotEvaluated, ConditionTextOf(transition), null, 0f
                    ));
                    continue;
                }

                var isEligible = transition.HasGuard == false || EvaluateGuard(transition, sourceNode, leaf);
                string liveValue = null;

                if (transition.HasGuard)
                {
                    GuardValueFormatter.TryFormat(
                          _context, _definition.GuardExpressions[transition.GuardIndex], out liveValue
                    );
                }

                result.Add(new GuardDebugInfo(
                      source, target, isEligible ? GuardMarker.Eligible : GuardMarker.False
                    , ConditionTextOf(transition), liveValue, 0f
                ));

                wonAlready |= isEligible;
            }
        }

        private string ConditionTextOf(in Transition transition)
        {
            if (transition.HasGuard)
            {
                return _definition.GuardExpressions[transition.GuardIndex] ?? "(guard)";
            }

            return (transition.Flags & TransitionFlags.Timed) != 0
                ? $"after {transition.MinDuration:0.##}s"
                : "(always)";
        }

        string IMachineDebug.NameOf(NodeIndex node)
            => _definition.NameOf(node);

        // ── registration ────────────────────────────────────────────────────────────────────────

        [Conditional(ValidationDefines.UNITY_EDITOR)]
        [Conditional(ValidationDefines.DEVELOPMENT_BUILD)]
        [Conditional(ValidationDefines.HFSM_DEBUG)]
        private void RegisterForDebugging()
        {
            _log ??= new TransitionLogEntry[LOG_CAPACITY];
            MachineDebugRegistry.Register(this);
        }

        [Conditional(ValidationDefines.UNITY_EDITOR)]
        [Conditional(ValidationDefines.DEVELOPMENT_BUILD)]
        [Conditional(ValidationDefines.HFSM_DEBUG)]
        private void UnregisterFromDebugging()
            => MachineDebugRegistry.Unregister(this);

        [Conditional(ValidationDefines.UNITY_EDITOR)]
        [Conditional(ValidationDefines.DEVELOPMENT_BUILD)]
        [Conditional(ValidationDefines.HFSM_DEBUG)]
        private void ClearLog()
        {
            _logHead = 0;
            _logCount = 0;
        }

        // ── recording ───────────────────────────────────────────────────────────────────────────

        [Conditional(ValidationDefines.UNITY_EDITOR)]
        [Conditional(ValidationDefines.DEVELOPMENT_BUILD)]
        [Conditional(ValidationDefines.HFSM_DEBUG)]
        private void RecordTransition(
              int source
            , int target
            , TransitionCause cause
            , int transitionIndex
            , float timeInSource
        )
        {
            if (_log == null)
            {
                return;
            }

            var trigger = transitionIndex >= 0
                ? _definition.Transitions[transitionIndex].Trigger
                : TriggerId.None;

            AppendLog(new TransitionLogEntry(
                  NodeIndex.Of(source)
                , NodeIndex.Of(target)
                , cause
                , transitionIndex
                , trigger
                , timeInSource
                , _timeInMachine
                , _frame
            ));
        }

        /// <summary>
        /// Records a queued trigger that matched nothing in the active configuration and was
        /// dropped. See <see cref="MachineError"/> §DEC-007 for why dropping rather than retaining
        /// it is the correct behaviour — this is what makes the drop visible instead of silent.
        /// </summary>
        [Conditional(ValidationDefines.UNITY_EDITOR)]
        [Conditional(ValidationDefines.DEVELOPMENT_BUILD)]
        [Conditional(ValidationDefines.HFSM_DEBUG)]
        private void RecordUnmatchedTrigger(TriggerId trigger)
        {
            if (_log == null)
            {
                return;
            }

            AppendLog(new TransitionLogEntry(
                  CurrentNode
                , NodeIndex.Invalid
                , TransitionCause.UnmatchedTrigger
                , -1
                , trigger
                , TimeInState
                , _timeInMachine
                , _frame
            ));
        }

        private void AppendLog(in TransitionLogEntry entry)
        {
            _log[_logHead] = entry;
            _logHead = (_logHead + 1) % _log.Length;

            if (_logCount < _log.Length)
            {
                _logCount++;
            }
        }

        [Conditional(ValidationDefines.UNITY_EDITOR)]
        [Conditional(ValidationDefines.DEVELOPMENT_BUILD)]
        [Conditional(ValidationDefines.HFSM_DEBUG)]
        private void LogWarning(MachineError error)
            => DevLogger.Default.LogWarning(error.Prefix(_definition.Name).ToString());
    }
}
