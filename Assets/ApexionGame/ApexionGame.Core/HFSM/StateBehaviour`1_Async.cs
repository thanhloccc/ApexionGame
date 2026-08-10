#if UNITASK || UNITY_6000_0_OR_NEWER

using System.Threading;

namespace ApexionGame.HFSM
{
#if UNITASK
    using UnityTask = Cysharp.Threading.Tasks.UniTask;
#else
    using UnityTask = UnityEngine.Awaitable;
#endif

    public abstract partial class StateBehaviour<TContext>
        where TContext : class
    {
        // The async half of the dispatch seam. Only reached when IsAsync is true, which only
        // AsyncStateBehaviour<TContext> sets — so the sync path never allocates a state machine.

        internal virtual UnityTask EnterAsyncCore(
              TContext context
            , byte[] blob
            , int offset
            , StateInfo info
            , CancellationToken token
        )
            => CompletedTask();

        internal virtual UnityTask ExitAsyncCore(
              TContext context
            , byte[] blob
            , int offset
            , StateInfo info
            , CancellationToken token
        )
            => CompletedTask();

        // An empty async body completes synchronously on both backends — UniTask and Awaitable are
        // each specifically optimized for exactly this shape — so a behaviour that does not
        // override the async hook costs no more than an ordinary virtual call.
        private protected static async UnityTask CompletedTask()
        {
        }
    }
}

#endif
