#if UNITASK || UNITY_6000_0_OR_NEWER

using System.Threading;

namespace ApexionGame.HFSM
{
#if UNITASK
    using UnityTask = Cysharp.Threading.Tasks.UniTask;
#else
    using UnityTask = UnityEngine.Awaitable;
#endif

    /// <summary>
    /// A state whose entry or exit takes time — an animation, a fade, a scene load.
    /// </summary>
    /// <remarks>
    /// Overriding either async hook makes every transition through this node asynchronous: the
    /// machine leaves <see cref="MachinePhase.Idle"/>, no node updates until the chain settles, and
    /// <see cref="AsyncPolicy"/> decides what a transition selected mid-flight does.
    /// <para>
    /// <b>Observe the token.</b> A hook that ignores cancellation simply finishes late and the
    /// machine discards the result — nothing corrupts, but the state's side effects still land.
    /// </para>
    /// </remarks>
    public abstract class AsyncStateBehaviour<TContext> : StateBehaviour<TContext>
        where TContext : class
    {
        internal sealed override bool IsAsync => true;

        protected virtual UnityTask OnEnterAsync(TContext context, StateInfo info, CancellationToken token)
            => CompletedTask();

        protected virtual UnityTask OnExitAsync(TContext context, StateInfo info, CancellationToken token)
            => CompletedTask();

        internal sealed override UnityTask EnterAsyncCore(
              TContext context
            , byte[] blob
            , int offset
            , StateInfo info
            , CancellationToken token
        )
            => OnEnterAsync(context, info, token);

        internal sealed override UnityTask ExitAsyncCore(
              TContext context
            , byte[] blob
            , int offset
            , StateInfo info
            , CancellationToken token
        )
            => OnExitAsync(context, info, token);
    }
}

#endif
