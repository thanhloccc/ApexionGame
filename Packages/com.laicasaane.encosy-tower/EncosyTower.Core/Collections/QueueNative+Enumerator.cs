using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections
{
    public partial struct QueueNative<T>
        where T : unmanaged
    {
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnly _queue;
            private readonly int _version;
            private int _index;
            private T _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal unsafe Enumerator(QueueUnsafe<T>* data, in ReadOnly queue)
            {
                _queue = queue;
                // SAFETY: data is the live header retained by the native owner/view.
                unsafe
                {
                    _version = data->_version;
                }
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                ThrowHelper.ThrowIfCollectionWasModified(_version == CurrentVersion());

                if ((uint)_index < (uint)_queue.Count)
                {
                    // SAFETY: The view's checked read keeps the header live; the ring index stays inside the buffer.
                    unsafe
                    {
                        var index = _queue.m_Data->_head + _index++;
                        if (index >= _queue.Capacity)
                        {
                            index -= _queue.Capacity;
                        }

                        _current = _queue.m_Data->_buffer.AsReadOnlySpan()[index];
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
                    return _queue.m_Data->_version;
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

                _index = 0;
                _current = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Dispose()
            {
            }
        }
    }
}
