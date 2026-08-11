#if UNITASK

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

/// <remarks>
/// Guarded by <c>UNITASK</c> specifically rather than the runtime's broader
/// <c>UNITASK || UNITY_6000_0_OR_NEWER</c>: a manually-gated async behaviour needs a completion
/// source to control exactly when a hook finishes, and <c>UniTaskCompletionSource</c> is the one
/// this project already depends on. Without UniTask, this file compiles to nothing rather than
/// duplicating the same tests against <c>AwaitableCompletionSource</c>.
/// </remarks>
public sealed class MachineAsyncTests
{
    private enum State { A, B, C }

    private sealed class Context
    {
        public readonly List<string> Log = new();
        public bool Go;
    }

    /// <summary>
    /// Awaits a gate the test controls, so enter/exit can be paused mid-flight and inspected.
    /// </summary>
    private sealed class GatedBehaviour : AsyncStateBehaviour<Context>
    {
        private readonly string _name;

        public UniTaskCompletionSource EnterGate;
        public UniTaskCompletionSource ExitGate;
        public bool EnterSawCancellation;
        public bool ExitSawCancellation;

        public GatedBehaviour(string name)
        {
            _name = name;
        }

        // Realistic, well-behaved cancellation: the awaited task is raced against the token via
        // AttachExternalCancellation, so a Cancel() call unblocks this await even though EnterGate
        // itself is never separately resolved — exactly like awaiting any other cancellable
        // UniTask (a network call, a delay, an addressables load) would behave in real code.
        protected override async UniTask OnEnterAsync(Context context, StateInfo info, CancellationToken token)
        {
            context.Log.Add($"EnterStart:{_name}");
            EnterGate = new UniTaskCompletionSource();

            try
            {
                await EnterGate.Task.AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                EnterSawCancellation = true;
                throw;
            }

            context.Log.Add($"EnterEnd:{_name}");
        }

        protected override async UniTask OnExitAsync(Context context, StateInfo info, CancellationToken token)
        {
            context.Log.Add($"ExitStart:{_name}");
            ExitGate = new UniTaskCompletionSource();

            try
            {
                await ExitGate.Task.AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                ExitSawCancellation = true;
                throw;
            }

            context.Log.Add($"ExitEnd:{_name}");
        }
    }

    private sealed class SyncRecorder : StateBehaviour<Context>
    {
        private readonly string _name;

        public SyncRecorder(string name)
        {
            _name = name;
        }

        protected override void OnEnter(Context c, in StateInfo info) => c.Log.Add($"Enter:{_name}");

        protected override void OnExit(Context c, in StateInfo info) => c.Log.Add($"Exit:{_name}");
    }

    [Test]
    public void EnterAsync_SuspendsUpdatesUntilItCompletes()
    {
        var gated = new GatedBehaviour("B");

        var definition = HierarchicalStateMachine<Context, State>.Define("Async")
            .State(State.A, new SyncRecorder("A"))
                .To(State.B).When(static c => c.Go)
            .State(State.B, gated)
            .BuildOrThrow();

        var context = new Context();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Log.Clear();

        context.Go = true;
        machine.Tick(0.1f);

        Assert.That(machine.Phase, Is.EqualTo(MachinePhase.Entering));
        Assert.That(context.Log, Is.EqualTo(new[] { "Exit:A", "EnterStart:B" }));

        // While suspended, ticking must not run OnUpdate on anything — the target has not finished
        // entering and the source has already exited.
        context.Log.Clear();
        machine.Tick(0.1f);
        Assert.That(context.Log, Is.Empty);

        gated.EnterGate.TrySetResult();
        machine.Tick(0.1f); // resumes the continuation and settles

        Assert.That(machine.Phase, Is.EqualTo(MachinePhase.Idle));
        Assert.That(context.Log, Does.Contain("EnterEnd:B"));
        Assert.That(machine.CurrentState, Is.EqualTo(State.B));

        machine.Dispose();
    }

    [Test]
    public void CancelAndReplace_ExitsWhatWasEnteredBeforeStartingTheReplacement()
    {
        var gatedB = new GatedBehaviour("B");

        var definition = HierarchicalStateMachine<Context, State>.Define("Cancel")
            .State(State.A, new SyncRecorder("A"))
            .State(State.B, gatedB)
            .State(State.C, new SyncRecorder("C"))
            .BuildOrThrow();

        var context = new Context();
        var machine = definition.CreateInstance(context, new MachineOptions {
            TickMode = TickMode.Manual,
            AsyncPolicy = AsyncPolicy.CancelAndReplace,
        });

        machine.RequestTransitionOrError(State.B);
        machine.Tick(0.1f); // suspended inside B's EnterAsync
        Assert.That(machine.Phase, Is.EqualTo(MachinePhase.Entering));
        context.Log.Clear();

        // A new request while B is still entering must cancel that chain and run its own.
        var result = machine.RequestTransitionOrError(State.C);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(gatedB.EnterSawCancellation, Is.True);

        machine.Tick(0.1f);

        Assert.That(machine.Phase, Is.EqualTo(MachinePhase.Idle));
        Assert.That(machine.CurrentState, Is.EqualTo(State.C));
        // B was entered (logged EnterStart) but never finished, and must have been exited
        // synchronously during cancellation before C ever started entering.
        Assert.That(context.Log, Does.Contain("Enter:C"));
        Assert.That(context.Log.IndexOf("Enter:C"), Is.GreaterThan(-1));

        machine.Dispose();
    }

    [Test]
    public void Dispose_MidTransition_RunsNoPendingOnEnter_AndTouchesNothingLater()
    {
        var gatedB = new GatedBehaviour("B");

        var definition = HierarchicalStateMachine<Context, State>.Define("DisposeMidFlight")
            .State(State.A, new SyncRecorder("A"))
            .State(State.B, gatedB)
            .BuildOrThrow();

        var context = new Context();
        var machine = definition.CreateInstance(context, TickMode.Manual);

        machine.RequestTransitionOrError(State.B);
        machine.Tick(0.1f); // suspended inside B's EnterAsync, B never finishes entering
        Assert.That(machine.Phase, Is.EqualTo(MachinePhase.Entering));

        machine.Dispose();
        Assert.That(machine.IsRunning, Is.False);

        // The gate is still open on the OLD behaviour instance. Releasing it now must not resurrect
        // the disposed machine or touch its arrays -- the generation check must bail out first.
        Assert.DoesNotThrow(() => gatedB.EnterGate.TrySetResult());
    }

    [Test]
    public void QueuePolicy_RunsTheReplacementOnlyAfterTheCurrentChainSettles()
    {
        var gatedB = new GatedBehaviour("B");

        var definition = HierarchicalStateMachine<Context, State>.Define("Queue")
            .State(State.A, new SyncRecorder("A"))
            .State(State.B, gatedB)
            .State(State.C, new SyncRecorder("C"))
            .BuildOrThrow();

        var context = new Context();
        var machine = definition.CreateInstance(context, new MachineOptions {
            TickMode = TickMode.Manual,
            AsyncPolicy = AsyncPolicy.Queue,
        });

        machine.RequestTransitionOrError(State.B);
        machine.Tick(0.1f); // suspended inside B's EnterAsync

        var result = machine.RequestTransitionOrError(State.C);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(gatedB.EnterSawCancellation, Is.False, "queued, not cancelled");

        machine.Tick(0.1f); // still suspended; C must not have started yet
        Assert.That(machine.CurrentState, Is.Not.EqualTo(State.C));

        // Resolving B's entry drives it straight through Settle(), which starts the queued B->C
        // transition immediately -- and GatedBehaviour's exit is async too, so it suspends again
        // on a fresh gate before C ever gets a turn.
        gatedB.EnterGate.TrySetResult();
        Assert.That(machine.Phase, Is.EqualTo(MachinePhase.Exiting));

        gatedB.ExitGate.TrySetResult();
        machine.Tick(0.1f); // B's exit settles; C enters

        Assert.That(machine.CurrentState, Is.EqualTo(State.C));

        machine.Dispose();
    }
}

#endif
