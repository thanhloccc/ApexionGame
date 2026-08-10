using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections
{
    public partial struct StackNative<T>
        where T : unmanaged
    {
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnly _stack;
            private readonly int _version;
            private int _index;
            private T _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal unsafe Enumerator(StackUnsafe<T>* data, in ReadOnly stack)
            {
                _stack = stack;
                // SAFETY: data is the live header retained by the native owner/view.
                unsafe
                {
                    _version = data->_version;
                }
                _index = stack.Count - 1;
                _current = default;
            }

            public bool MoveNext()
            {
                ThrowHelper.ThrowIfCollectionWasModified(_version == CurrentVersion());

                if (_index >= 0)
                {
                    // SAFETY: The borrowed view keeps the native buffer live while the current item is read.
                    unsafe
                    {
                        _current = _stack.m_Data->_buffer.AsReadOnlySpan()[_index--];
                    }
                    return true;
                }
                _current = default;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly int CurrentVersion()
            {
                // SAFETY: The view safety check and owner lifetime keep the header live.
                unsafe
                {
                    return _stack.m_Data->_version;
                }
            }

            public readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            readonly object IEnumerator.Current
                => Current;

            public void Reset()
            {
                ThrowHelper.ThrowIfCollectionWasModified(_version == CurrentVersion());

                _index = _stack.Count - 1;
                _current = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Dispose()
            {
            }
        }
    }
}
