using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace EncosyTower.StringIds
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public partial struct StringVaultNative : IStringVault
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        , IReadOnlyList<UnmanagedString>
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe StringVaultUnsafe* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

#if UNITY_BURST
        private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
            = Unity.Burst.SharedStatic<int>.GetOrCreate<StringVaultNative>();
#else
        private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

        public StringVaultNative(
              int initialCapacity
            , AllocatorStrategy allocator
            , bool allowEmptyString = false
        )
            : this()
        {
            // SAFETY: The allocation returns an owned native vault header kept alive by this container.
            unsafe
            {
                m_Data = StringVaultUnsafe.Alloc(initialCapacity, allocator, allowEmptyString);
            }
            CreateSafety(allocator.ToAllocator());
        }

        public StringVaultNative(
              ReadOnlySpan<UnmanagedString> strings
            , AllocatorStrategy allocator
            , bool allowEmptyString = false
        )
            : this(strings.Length, allocator, allowEmptyString)
        {
            foreach (var str in strings)
            {
                GetOrMakeId(str);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateSafety(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = EncosyCollectionSafetyAPI.CreateSafetyHandle(allocator);

            EncosyCollectionSafetyAPI.SetStaticSafetyId<StringVaultNative>(
                  ref m_Safety
#if UNITY_BURST
                , ref s_SafetyId.Data
#else
                , ref s_SafetyId
#endif
            );

            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: The pointer is checked for null before observing the owned vault header.
                unsafe
                {
                    return m_Data != null && m_Data->IsCreated;
                }
            }
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                // SAFETY: CheckRead validates the owner before reading the live vault header.
                unsafe
                {
                    return m_Data->Capacity;
                }
            }
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                // SAFETY: CheckRead validates the owner before reading the live vault header.
                unsafe
                {
                    return m_Data->Count;
                }
            }
        }

        public readonly bool AllowEmptyString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                // SAFETY: CheckRead validates the owner before reading the live vault header.
                unsafe
                {
                    return m_Data->AllowEmptyString;
                }
            }
        }

        public readonly UnmanagedString this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                // SAFETY: CheckRead validates the owner and the vault performs its own index validation.
                unsafe
                {
                    return (*m_Data)[index];
                }
            }
        }

        public void Dispose()
        {
            // SAFETY: The native vault header is owned by this container and released exactly once.
            unsafe
            {
                if (m_Data == null)
                {
                    return;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (AtomicSafetyHandle.IsDefaultValue(m_Safety) == false)
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }

            EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref m_Safety);
#endif

                StringVaultUnsafe.Free(m_Data);
                m_Data = null;
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (AtomicSafetyHandle.IsDefaultValue(m_Safety) == false)
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif

            if (IsCreated == false)
            {
                return inputDeps;
            }

            // SAFETY: The scheduled job receives the live vault header and takes ownership of its release.
            unsafe
            {
                var handle = new StringVaultNativeDisposeJob {
                    _data = new StringVaultNativeDispose {
                        m_Data = m_Data,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        m_Safety = m_Safety,
#endif
                    },
                }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                EncosyCollectionSafetyAPI.ReleaseSafetyHandleAfterSchedule(ref m_Safety);
#endif

                m_Data = null;
                return handle;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the owner before mutating the live vault.
            unsafe
            {
                m_Data->Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringId GetOrMakeId(in UnmanagedString str)
        {
            CheckWriteAndBumpSecondaryVersion();
            // SAFETY: CheckWrite validates the owner before mutating the live vault.
            unsafe
            {
                return m_Data->GetOrMakeId(str);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Option<StringId> TryGetId(in UnmanagedString str)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before reading the live vault.
            unsafe
            {
                return m_Data->TryGetId(str);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetId(in UnmanagedString str, out StringId result)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before reading the live vault.
            unsafe
            {
                return m_Data->TryGetId(str, out result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Option<UnmanagedString> TryGetUnmanagedString(StringId id)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before reading the live vault.
            unsafe
            {
                return m_Data->TryGetUnmanagedString(id);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetUnmanagedString(StringId id, out UnmanagedString result)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before reading the live vault.
            unsafe
            {
                return m_Data->TryGetUnmanagedString(id, out result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsId(StringId id)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before reading the live vault.
            unsafe
            {
                return m_Data->ContainsId(id);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<UnmanagedString> destination)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before copying from the live vault.
            unsafe
            {
                m_Data->CopyTo(destination);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<UnmanagedString> destination, int length)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before copying from the live vault.
            unsafe
            {
                m_Data->CopyTo(destination, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(int sourceStartIndex, Span<UnmanagedString> destination)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before copying from the live vault.
            unsafe
            {
                m_Data->CopyTo(sourceStartIndex, destination);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(
              int sourceStartIndex
            , Span<UnmanagedString> destination
            , int length
        )
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before copying from the live vault.
            unsafe
            {
                m_Data->CopyTo(sourceStartIndex, destination, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<UnmanagedString> destination)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before copying from the live vault.
            unsafe
            {
                return m_Data->TryCopyTo(destination);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<UnmanagedString> destination, int length)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before copying from the live vault.
            unsafe
            {
                return m_Data->TryCopyTo(destination, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(int sourceStartIndex, Span<UnmanagedString> destination)
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before copying from the live vault.
            unsafe
            {
                return m_Data->TryCopyTo(sourceStartIndex, destination);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(
              int sourceStartIndex
            , Span<UnmanagedString> destination
            , int length
        )
        {
            CheckRead();
            // SAFETY: CheckRead validates the owner before copying from the live vault.
            unsafe
            {
                return m_Data->TryCopyTo(sourceStartIndex, destination, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityBy(int amount)
        {
            CheckWriteAndBumpSecondaryVersion();
            // SAFETY: CheckWrite validates the owner before mutating the live vault.
            unsafe
            {
                return m_Data->IncreaseCapacityBy(amount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityTo(int newCapacity)
        {
            CheckWriteAndBumpSecondaryVersion();
            // SAFETY: CheckWrite validates the owner before mutating the live vault.
            unsafe
            {
                return m_Data->IncreaseCapacityTo(newCapacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator()
        {
            CheckRead();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // SAFETY: CheckRead validates the owner before borrowing its live header for enumeration.
            unsafe
            {
                return new(m_Data, m_Safety);
            }
#else
            // SAFETY: CheckRead validates the owner before borrowing its live header for enumeration.
            unsafe
            {
                return new(m_Data);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly IEnumerator<UnmanagedString> IEnumerable<UnmanagedString>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckWriteAndBumpSecondaryVersion()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
        }
    }

#if UNITY_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct StringVaultNativeDisposeJob : IJob
    {
        internal StringVaultNativeDispose _data;

        public readonly void Execute()
            => _data.Dispose();
    }

    [NativeContainer]
    internal struct StringVaultNativeDispose
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe StringVaultUnsafe* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        public readonly void Dispose()
        {
            // SAFETY: The dispose job owns this vault header and releases it exactly once.
            unsafe
            {
                StringVaultUnsafe.Free(m_Data);
            }
        }
    }
}
