using Unity.Collections.LowLevel.Unsafe;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// A state behaviour that owns a struct of per-instance data.
    /// </summary>
    /// <typeparam name="TData">
    /// Laid out at a fixed offset in each instance's blob. Always starts zeroed — the machine clears
    /// the blob on creation and on <c>Reset</c>, so <see cref="OnEnter"/> does not have to
    /// defensively initialise every field.
    /// </typeparam>
    /// <remarks>
    /// The <see langword="ref"/> reaches straight into the instance's <see cref="byte"/> array.
    /// <see cref="UnsafeUtility.As{U, T}(ref U)"/> yields a GC-tracked managed reference, so the
    /// array is never pinned and the collector may still move it.
    /// <para>
    /// One behaviour instance placed at two different nodes gets two independent slots — correct,
    /// but worth knowing before it surprises someone.
    /// </para>
    /// </remarks>
    public abstract class StateBehaviour<TContext, TData> : StateBehaviour<TContext>
        where TContext : class
        where TData : unmanaged
    {
        /// <inheritdoc cref="StateBehaviour{TContext}.OnEnter"/>
        protected virtual void OnEnter(TContext context, ref TData data, in StateInfo info)
        {
        }

        /// <inheritdoc cref="StateBehaviour{TContext}.OnUpdate"/>
        protected virtual void OnUpdate(TContext context, ref TData data, in StateInfo info, float deltaTime)
        {
        }

        /// <inheritdoc cref="StateBehaviour{TContext}.OnExit"/>
        protected virtual void OnExit(TContext context, ref TData data, in StateInfo info)
        {
        }

        internal sealed override int DataSize => UnsafeUtility.SizeOf<TData>();

        internal sealed override int DataAlign => UnsafeUtility.AlignOf<TData>();

        internal sealed override void EnterCore(TContext context, byte[] blob, int offset, in StateInfo info)
            => OnEnter(context, ref UnsafeUtility.As<byte, TData>(ref blob[offset]), info);

        internal sealed override void UpdateCore(
              TContext context
            , byte[] blob
            , int offset
            , in StateInfo info
            , float deltaTime
        )
            => OnUpdate(context, ref UnsafeUtility.As<byte, TData>(ref blob[offset]), info, deltaTime);

        internal sealed override void ExitCore(TContext context, byte[] blob, int offset, in StateInfo info)
            => OnExit(context, ref UnsafeUtility.As<byte, TData>(ref blob[offset]), info);
    }
}
