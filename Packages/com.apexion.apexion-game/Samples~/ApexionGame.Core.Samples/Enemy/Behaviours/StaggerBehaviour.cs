#if UNITASK

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ApexionGame.HFSM.Samples;

/// <summary>
/// Reached by <see cref="EnemyTrigger.Staggered"/> from anywhere. Gates its own entry behind a
/// <see cref="UniTaskCompletionSource"/> the playground controls directly, so button 8 can drive
/// "cancelled mid-flight" deterministically instead of racing real frames — the same technique
/// <c>MachineAsyncTests.GatedBehaviour</c> uses.
/// </summary>
public sealed class StaggerBehaviour : AsyncStateBehaviour<EnemyContext>
{
    public UniTaskCompletionSource EnterGate;

    protected override async UniTask OnEnterAsync(EnemyContext context, StateInfo info, CancellationToken token)
    {
        context.Log.Add("EnterStart:Stagger");
        EnterGate = new UniTaskCompletionSource();

        try
        {
            await EnterGate.Task.AttachExternalCancellation(token);
        }
        catch (OperationCanceledException)
        {
            context.Log.Add("EnterCancelled:Stagger");
            throw;
        }

        context.Log.Add("EnterEnd:Stagger");
    }
}

#endif
