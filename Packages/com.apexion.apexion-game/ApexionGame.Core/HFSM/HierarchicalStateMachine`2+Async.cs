using System;
using System.Threading;
#if UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace ApexionGame.HFSM
{
#if UNITASK
    using UnityTask = Cysharp.Threading.Tasks.UniTask;
#elif UNITY_6000_0_OR_NEWER
    using UnityTask = UnityEngine.Awaitable;
#endif

    partial class HierarchicalStateMachine<TContext, TState>
    {
#if UNITASK || UNITY_6000_0_OR_NEWER

        private int[] _asyncExitPlan;
        private int[] _asyncEnterPlan;
        private CancellationTokenSource _transitionCts;
        private int _queuedTarget = -1;

        /// <summary>
        /// Takes over the transition when any node in either plan enters or exits asynchronously.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> when nothing in the plan is async, which is the common case and
        /// leaves the caller on the allocation-free synchronous path.
        /// </returns>
        private bool TryStartAsyncTransition(
              int source
            , int target
            , int actionIndex
            , int transitionIndex
            , TransitionCause cause
            , int previousLeaf
            , float timeInSource
        )
        {
            if (PlanHasAsync() == false)
            {
                return false;
            }

            _asyncExitPlan ??= new int[_nodes.Length];
            _asyncEnterPlan ??= new int[_nodes.Length];

            var exitCount = _exitCount;
            var enterCount = _enterCount;

            System.Array.Copy(_exitPlan, _asyncExitPlan, exitCount);
            System.Array.Copy(_enterPlan, _asyncEnterPlan, enterCount);

            // The shared plan buffers are free again the moment they are copied, so a replacement
            // transition can be planned while this one is still in flight.
            _exitCount = 0;
            _enterCount = 0;

            _transitionCts?.Dispose();
            _transitionCts = new CancellationTokenSource();

            var task = RunTransitionAsync(
                  exitCount
                , enterCount
                , actionIndex
                , source
                , target
                , transitionIndex
                , cause
                , previousLeaf
                , timeInSource
                , _generation
                , _transitionCts.Token
            );

#if UNITASK
            task.Forget();
#else
            _ = task;
#endif
            return true;
        }

        private bool PlanHasAsync()
        {
            for (var i = 0; i < _exitCount; i++)
            {
                if (IsAsyncNode(_exitPlan[i]))
                {
                    return true;
                }
            }

            for (var i = 0; i < _enterCount; i++)
            {
                if (IsAsyncNode(_enterPlan[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAsyncNode(int node)
        {
            var behaviourIndex = _nodes[node].BehaviourIndex;
            return behaviourIndex >= 0 && _definition.Behaviours[behaviourIndex].IsAsync;
        }

        private async UnityTask RunTransitionAsync(
              int exitCount
            , int enterCount
            , int actionIndex
            , int source
            , int target
            , int transitionIndex
            , TransitionCause cause
            , int previousLeaf
            , float timeInSource
            , int generation
            , CancellationToken token
        )
        {
            _phase = MachinePhase.Exiting;

            for (var i = 0; i < exitCount; i++)
            {
                var node = _asyncExitPlan[i];

                if (token.IsCancellationRequested == false)
                {
                    try
                    {
                        await InvokeExitAsync(node, token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // A well-behaved hook observes cancellation by awaiting something
                        // cancellable (e.g. via AttachExternalCancellation) rather than by polling
                        // the token, and that throws instead of returning normally. Treat it the
                        // same as an observed-but-not-thrown cancellation — token.IsCancellationRequested
                        // is already true by construction, so the check below takes over from here.
                    }

                    // The await may have resumed after Dispose() returned this instance to the pool
                    // and a different CreateInstance() call handed it to a new owner. Touching any
                    // field past this point would corrupt that owner's state, so stop immediately —
                    // this instance is no longer "this transition"'s to finish.
                    if (_generation != generation)
                    {
                        return;
                    }
                }
                else
                {
                    InvokeExit(node);
                }

                ClearActive(node);

                if (_nodes[node].IsSingleChildContainer)
                {
                    _activeChild[node] = -1;
                }
            }

            if (token.IsCancellationRequested)
            {
                // Nothing has been entered yet, so the configuration is simply the one above the
                // exited branch. Settling here is a defined state, not a half-entered one.
                Settle();
                return;
            }

            InvokeAction(actionIndex);
            _phase = MachinePhase.Entering;

            var entered = 0;

            for (var i = 0; i < enterCount; i++)
            {
                var node = _asyncEnterPlan[i];

                if (token.IsCancellationRequested)
                {
                    break;
                }

                var parent = _nodes[node].Parent;

                if (parent >= 0 && _nodes[parent].Kind != StateNodeKind.Parallel)
                {
                    _activeChild[parent] = node;
                }

                SetActive(node);
                _timeInNode[node] = 0f;

                try
                {
                    await InvokeEnterAsync(node, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Same reasoning as the exit loop above.
                }

                if (_generation != generation)
                {
                    return;
                }

                entered = i + 1;
            }

            if (token.IsCancellationRequested)
            {
                // Unwind exactly what was entered, innermost first, synchronously — a cancelled
                // chain does not get to await its way out.
                for (var i = entered - 1; i >= 0; i--)
                {
                    var node = _asyncEnterPlan[i];
                    InvokeExit(node);
                    ClearActive(node);

                    if (_nodes[node].IsSingleChildContainer)
                    {
                        _activeChild[node] = -1;
                    }
                }

                Settle();
                return;
            }

            RaiseStateChanged(previousLeaf, cause);
            RecordTransition(source, target, cause, transitionIndex, timeInSource);
            Settle();
        }

        /// <summary>
        /// Returns the machine to <see cref="MachinePhase.Idle"/> and runs whatever was queued
        /// behind the chain that just finished.
        /// </summary>
        private void Settle()
        {
            if (_phase == MachinePhase.Stopped)
            {
                return;
            }

            _phase = MachinePhase.Idle;

            if (_queuedTarget < 0)
            {
                return;
            }

            var target = _queuedTarget;
            _queuedTarget = -1;
            ExecuteTransition(PrimaryLeaf(), target, -1, TransitionCause.Request);
        }

        private async UnityTask InvokeEnterAsync(int node, CancellationToken token)
        {
            var current = _nodes[node];

            if (current.BehaviourIndex < 0)
            {
                return;
            }

            var behaviour = _definition.Behaviours[current.BehaviourIndex];
            var info = InfoFor(node);

            if (behaviour.IsAsync)
            {
                await behaviour.EnterAsyncCore(_context, _stateData, current.StateDataOffset, info, token);
                return;
            }

            behaviour.EnterCore(_context, _stateData, current.StateDataOffset, info);
        }

        private async UnityTask InvokeExitAsync(int node, CancellationToken token)
        {
            var current = _nodes[node];

            if (current.BehaviourIndex < 0)
            {
                return;
            }

            var behaviour = _definition.Behaviours[current.BehaviourIndex];
            var info = InfoFor(node);

            if (behaviour.IsAsync)
            {
                await behaviour.ExitAsyncCore(_context, _stateData, current.StateDataOffset, info, token);
                return;
            }

            behaviour.ExitCore(_context, _stateData, current.StateDataOffset, info);
        }

        /// <summary>
        /// Applies <see cref="AsyncPolicy"/> to a transition requested while a chain is in flight.
        /// </summary>
        /// <returns><see langword="true"/> when the caller should proceed with the transition now.</returns>
        private bool AcceptWhileInFlight(int target)
        {
            switch (_asyncPolicy)
            {
                case AsyncPolicy.CancelAndReplace:
                    CancelInFlight();
                    return true;

                case AsyncPolicy.Queue:
                    _queuedTarget = target;
                    return false;

                default:
                    return false;
            }
        }

        private void CancelInFlight()
        {
            if (_transitionCts == null)
            {
                return;
            }

            _transitionCts.Cancel();
            _transitionCts.Dispose();
            _transitionCts = null;
            _queuedTarget = -1;
        }

#else

        private bool TryStartAsyncTransition(
              int source
            , int target
            , int actionIndex
            , int transitionIndex
            , TransitionCause cause
            , int previousLeaf
            , float timeInSource
        )
            => false;

        private bool AcceptWhileInFlight(int target)
            => false;

        private void CancelInFlight()
        {
        }

#endif
    }
}
