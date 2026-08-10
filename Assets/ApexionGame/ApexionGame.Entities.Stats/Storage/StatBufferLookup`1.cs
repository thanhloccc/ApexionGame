using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Common;
using Unity.Collections.LowLevel.Unsafe;

namespace ApexionGame.Entities.Stats
{
    /// <summary>
    /// Resolves a <see cref="StatOwnerHandle"/> to the buffer of <typeparamref name="T"/> owned by it.
    /// </summary>
    /// <remarks>
    /// Generic over exactly one element type, mirroring Unity.Entities BufferLookup&lt;T&gt;, so that
    /// <see cref="StatReader{TValuePair, TStat}"/> stays generic over two types only.
    /// Borrows the safety handle of the store it came from; it never owns memory.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct StatBufferLookup<T> : IIsCreated
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<StatOwnerSlot>* m_Slots;

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<StatBufferSlot<T>>* m_Buffers;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Slots != null && m_Buffers != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetBuffer(StatOwnerHandle owner, out StatBuffer<T> buffer)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (IsCreated == false || owner.IsValid == false)
            {
                buffer = default;
                return false;
            }

            // SAFETY: The checks above validate both live lists; the index is range-checked against
            // the slot list before either pointer is formed. Both lists are kept the same length by
            // StatStore, so one bounds check covers both.
            if ((uint)owner.Index >= (uint)m_Slots->Length)
            {
                buffer = default;
                return false;
            }

            if (m_Slots->Ptr[owner.Index].version != owner.Version)
            {
                buffer = default;
                return false;
            }

            var list = m_Buffers->Ptr[owner.Index].list;

            if (list == null)
            {
                buffer = default;
                return false;
            }

            buffer = new StatBuffer<T>(list);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            buffer.m_Safety = m_Safety;
#endif
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Exists(StatOwnerHandle owner)
            => TryGetBuffer(owner, out _);
    }
}
