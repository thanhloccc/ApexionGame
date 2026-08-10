using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

using ThrowHelper = EncosyTower.Collections.ThrowHelper;

namespace EncosyTower.StringIds
{
    partial struct StringVaultNative
    {
        public struct Enumerator : IEnumerator<UnmanagedString>
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly unsafe StringVaultUnsafe* _data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private readonly AtomicSafetyHandle _safety;
#endif

            private System.Range _current;
            private int _index;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal unsafe Enumerator(StringVaultUnsafe* data, AtomicSafetyHandle safety)
            {
                // SAFETY: The enumerator borrows a live vault header from its owning container.
                unsafe
                {
                    _data = data;
                }
                _safety = safety;
                _current = default;
                _index = 0;
            }
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal unsafe Enumerator(StringVaultUnsafe* data)
            {
                // SAFETY: The enumerator borrows a live vault header from its owning container.
                unsafe
                {
                    _data = data;
                }
                _current = default;
                _index = 0;
            }
#endif

            public bool MoveNext()
            {
                CheckRead();

                // SAFETY: CheckRead validates the owner and the enumerator bounds its index before reading the vault.
                unsafe
                {
                    if ((uint)_index < (uint)_data->_count)
                    {
                        _current = _data->_stringRanges[_index++];
                        return true;
                    }

                    _index = _data->_count + 1;
                    _current = default;
                    return false;
                }
            }

            public readonly UnmanagedString Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the owner and the current range was produced by MoveNext.
                    unsafe
                    {
                        return UnmanagedString.FromBufferAt(
                              _current
                            , _data->_stringBuffer.AsReadOnlySpan()[.._data->_stringBufferLength]
                        ).GetValueOrThrow();
                    }
                }
            }

            public void Reset()
            {
                CheckRead();
                _index = 0;
                _current = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Dispose() { }

            readonly object IEnumerator.Current
            {
                get
                {
                    CheckRead();

                    // SAFETY: CheckRead validates the owner before reading the enumerator's live vault header.
                    unsafe
                    {
                        ThrowHelper.ThrowIfEnumeratorOperationIsInvalid(_index != 0 && _index != _data->_count + 1);

                        return Current;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(_safety);
#endif
            }
        }
    }
}
