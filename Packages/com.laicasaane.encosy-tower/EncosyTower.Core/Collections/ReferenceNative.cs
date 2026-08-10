using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace EncosyTower.Collections
{
    /// <summary>
    /// The safe counterpart of <see cref="ReferenceUnsafe{T}"/>. It stores a single
    /// unmanaged value and guards every access with an <see cref="AtomicSafetyHandle"/>,
    /// mirroring <see cref="Unity.Collections.NativeReference{T}"/>.
    /// <br/>
    /// The raw storage and allocation live in the inlined <see cref="ReferenceUnsafe{T}"/>;
    /// this type only adds the one safety handle on top.
    /// </summary>
    /// <typeparam name="T">The type of the stored value.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Value = {Value}")]
    public struct ReferenceNative<T>
        : IDisposable, IEquatable<ReferenceNative<T>>
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        internal ReferenceUnsafe<T> m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

#if UNITY_BURST
        private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
            = Unity.Burst.SharedStatic<int>.GetOrCreate<ReferenceNative<T>>();
#else
        private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReferenceNative(
              AllocatorStrategy allocator
            , NativeArrayOptions options = NativeArrayOptions.ClearMemory
        )
        {
            Allocate(new ReferenceUnsafe<T>(allocator, options), allocator, out this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReferenceNative(T value, AllocatorStrategy allocator)
        {
            Allocate(new ReferenceUnsafe<T>(value, allocator), allocator, out this);
        }

        private static void Allocate(
              ReferenceUnsafe<T> data
            , AllocatorStrategy allocator
            , out ReferenceNative<T> self
        )
        {
            self = default;
            self.m_Data = data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            self.m_Safety = EncosyCollectionSafetyAPI.CreateSafetyHandle(allocator.ToAllocator());

            EncosyCollectionSafetyAPI.SetStaticSafetyId<ReferenceNative<T>>(
                  ref self.m_Safety
#if UNITY_BURST
                , ref s_SafetyId.Data
#else
                , ref s_SafetyId
#endif
            );

            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(self.m_Safety, true);
#endif
        }

        /// <summary>
        /// Whether this reference has been allocated (and not yet disposed).
        /// </summary>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.IsCreated;
        }

        /// <summary>
        /// The value stored in this reference.
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: The native safety handle check above validates the lifetime before reading the raw value.
                unsafe
                {
                    return m_Data.Value;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                // SAFETY: The native safety handle check above validates the lifetime before writing the raw value.
                unsafe
                {
                    m_Data.Value = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(ReferenceNative<T> reference)
            => Copy(this, reference);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(ReferenceNative<T> reference)
            => Copy(reference, this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(ReferenceNative<T> dst, ReferenceNative<T> src)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
#endif
            ReferenceUnsafe<T>.Copy(dst.m_Data, src.m_Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(ReferenceNative<T> other)
            => Value.Equals(other.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object obj)
            => obj is ReferenceNative<T> other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode()
            => Value.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ReferenceNative<T> left, ReferenceNative<T> right)
            => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ReferenceNative<T> left, ReferenceNative<T> right)
            => !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnly AsReadOnly()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = m_Safety;
            // SAFETY: The copied safety handle and native lifetime make this borrowed alias valid.
            unsafe
            {
                return new ReadOnly(m_Data.AsReadOnly(), ref safety);
            }
#else
            // SAFETY: The alias borrows the live raw allocation owned by this reference.
            unsafe
            {
                return new ReadOnly(m_Data.AsReadOnly());
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnly(ReferenceNative<T> reference)
            => reference.AsReadOnly();

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (AtomicSafetyHandle.IsDefaultValue(m_Safety) == false)
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif

            if (m_Data.IsCreated == false)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref m_Safety);
#endif

            m_Data.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (AtomicSafetyHandle.IsDefaultValue(m_Safety) == false)
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif

            if (m_Data.IsCreated == false)
            {
                return inputDeps;
            }

            // Schedule the underlying free first, then release the handle on the main
            // thread right after scheduling, matching NativeReference.Dispose(JobHandle).
            var jobHandle = new ReferenceNativeDisposeJob<T> {
                _data = new ReferenceNativeDispose<T> {
                    m_Data = m_Data,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = m_Safety,
#endif
                },
            }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            EncosyCollectionSafetyAPI.ReleaseSafetyHandleAfterSchedule(ref m_Safety);
#endif

            m_Data = default;
            return jobHandle;
        }

        /// <summary>
        /// A read-only alias for the value of a <see cref="ReferenceNative{T}"/>.
        /// Does not own any storage.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public readonly struct ReadOnly : IIsCreated
        {
#pragma warning disable IDE1006 // Naming Styles
            internal readonly ReferenceUnsafe<T>.ReadOnly m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal readonly AtomicSafetyHandle m_Safety;

#if UNITY_BURST
            private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
                = Unity.Burst.SharedStatic<int>.GetOrCreate<ReadOnly>();
#else
            private static int s_SafetyId;
#endif
#pragma warning restore IDE1006 // Naming Styles

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnly(in ReferenceUnsafe<T>.ReadOnly data, ref AtomicSafetyHandle safety)
            {
                m_Data = data;
                m_Safety = safety;

                EncosyCollectionSafetyAPI.SetStaticSafetyId<ReadOnly>(
                      ref m_Safety
#if UNITY_BURST
                    , ref s_SafetyId.Data
#else
                    , ref s_SafetyId
#endif
                );
            }
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnly(in ReferenceUnsafe<T>.ReadOnly data)
            {
                m_Data = data;
            }
#endif

            public bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.IsCreated;
            }

            public T Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    // SAFETY: The native safety handle check above validates the lifetime before reading the raw value.
                    unsafe
                    {
                        return m_Data.Value;
                    }
                }
            }
        }
    }

#if UNITY_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct ReferenceNativeDisposeJob<T> : IJob
        where T : unmanaged
    {
        internal ReferenceNativeDispose<T> _data;

        public void Execute()
            => _data.Dispose();
    }

    [NativeContainer]
    internal struct ReferenceNativeDispose<T>
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        internal ReferenceUnsafe<T> m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        public void Dispose()
            => m_Data.Dispose();
    }
}
