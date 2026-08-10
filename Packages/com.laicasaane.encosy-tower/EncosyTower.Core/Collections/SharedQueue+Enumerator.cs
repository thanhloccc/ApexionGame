using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EncosyTower.Collections
{
    public partial class SharedQueue<T, TNative>
        where T : unmanaged
        where TNative : unmanaged
    {
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnly _queue;
            private readonly int _version;
            private int _index;
            private T _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(ReadOnly queue)
            {
                _queue = queue;
                _version = queue._queue._version.ValueRO;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                ThrowHelper.ThrowIfCollectionWasModified(_version == _queue._queue._version.ValueRO);

                if ((uint)_index < (uint)_queue.Count)
                {
                    var values = _queue.ToArray();
                    _current = values[_index++];
                    return true;
                }
                _current = default;
                return false;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
            object IEnumerator.Current
                => Current;

            public void Reset()
            {
                ThrowHelper.ThrowIfCollectionWasModified(_version == _queue._queue._version.ValueRO);

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
