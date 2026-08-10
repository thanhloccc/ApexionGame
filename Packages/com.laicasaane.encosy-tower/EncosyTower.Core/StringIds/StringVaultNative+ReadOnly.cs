using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.StringIds
{
    partial struct StringVaultNative
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnly AsReadOnly()
            => new(this);

        [StructLayout(LayoutKind.Sequential)]
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public readonly struct ReadOnly : IReadOnlyStringVault
            , IReadOnlyList<UnmanagedString>
        {
#pragma warning disable IDE1006 // Naming Styles
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe StringVaultUnsafe* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal readonly AtomicSafetyHandle m_Safety;

#if UNITY_BURST
            private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
                = Unity.Burst.SharedStatic<int>.GetOrCreate<ReadOnly>();
#else
            private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnly(in StringVaultNative vault)
            {
                // SAFETY: The read-only view borrows the live vault header from its owner.
                unsafe
                {
                    m_Data = vault.m_Data;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = vault.m_Safety;

                EncosyCollectionSafetyAPI.SetStaticSafetyId<ReadOnly>(
                      ref m_Safety
#if UNITY_BURST
                    , ref s_SafetyId.Data
#else
                    , ref s_SafetyId
#endif
                );
#endif
            }

            public bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: The pointer is checked for null before observing the borrowed vault header.
                    unsafe
                    {
                        return m_Data != null && m_Data->IsCreated;
                    }
                }
            }

            public int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the borrowed vault before reading its header.
                    unsafe
                    {
                        return m_Data->Capacity;
                    }
                }
            }

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the borrowed vault before reading its header.
                    unsafe
                    {
                        return m_Data->Count;
                    }
                }
            }

            public bool AllowEmptyString
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the borrowed vault before reading its header.
                    unsafe
                    {
                        return m_Data->AllowEmptyString;
                    }
                }
            }

            public UnmanagedString this[int index]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Option<StringId> TryGetId(in UnmanagedString str)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before reading it.
                unsafe
                {
                    return m_Data->TryGetId(str);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetId(in UnmanagedString str, out StringId result)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before reading it.
                unsafe
                {
                    return m_Data->TryGetId(str, out result);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Option<UnmanagedString> TryGetUnmanagedString(StringId id)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before reading it.
                unsafe
                {
                    return m_Data->TryGetUnmanagedString(id);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetUnmanagedString(StringId id, out UnmanagedString result)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before reading it.
                unsafe
                {
                    return m_Data->TryGetUnmanagedString(id, out result);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ContainsId(StringId id)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before reading it.
                unsafe
                {
                    return m_Data->ContainsId(id);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(Span<UnmanagedString> destination)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before copying from it.
                unsafe
                {
                    m_Data->CopyTo(destination);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(Span<UnmanagedString> destination, int length)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before copying from it.
                unsafe
                {
                    m_Data->CopyTo(destination, length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(int sourceStartIndex, Span<UnmanagedString> destination)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before copying from it.
                unsafe
                {
                    m_Data->CopyTo(sourceStartIndex, destination);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(
                  int sourceStartIndex
                , Span<UnmanagedString> destination
                , int length
            )
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before copying from it.
                unsafe
                {
                    m_Data->CopyTo(sourceStartIndex, destination, length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCopyTo(Span<UnmanagedString> destination)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before copying from it.
                unsafe
                {
                    return m_Data->TryCopyTo(destination);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCopyTo(Span<UnmanagedString> destination, int length)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before copying from it.
                unsafe
                {
                    return m_Data->TryCopyTo(destination, length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCopyTo(int sourceStartIndex, Span<UnmanagedString> destination)
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before copying from it.
                unsafe
                {
                    return m_Data->TryCopyTo(sourceStartIndex, destination);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCopyTo(
                  int sourceStartIndex
                , Span<UnmanagedString> destination
                , int length
            )
            {
                CheckRead();
                // SAFETY: CheckRead validates the borrowed vault before copying from it.
                unsafe
                {
                    return m_Data->TryCopyTo(sourceStartIndex, destination, length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator()
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
            IEnumerator<UnmanagedString> IEnumerable<UnmanagedString>.GetEnumerator()
                => GetEnumerator();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(in StringVaultNative vault)
                => new(vault);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            }
        }
    }
}
