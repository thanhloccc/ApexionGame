using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EncosyTower.Collections
{
    partial class SharedList<T, TNative>
    {
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnly _list;
            private int _index;
            private readonly int _version;
            private T _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(ReadOnly list)
            {
                _list = list;
                _index = 0;
                _version = list._version[0];
                _current = default;
            }

            public bool MoveNext()
            {
                var localList = _list;

                if (_version == localList.Version && ((uint)_index < (uint)localList.Count))
                {
                    _current = localList._buffer[_index];
                    _index++;
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                ThrowHelper.ThrowIfCollectionWasModified(_version == _list.Version);

                _index = _list.Count + 1;
                _current = default;
                return false;
            }

            public readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            public void Reset()
            {
                ThrowHelper.ThrowIfCollectionWasModified(_version == _list.Version);

                _index = 0;
                _current = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Dispose()
            {
            }

            readonly object IEnumerator.Current
            {
                get
                {
                    ThrowHelper.ThrowIfEnumeratorOperationIsInvalid(_index != 0 && _index != _list.Count + 1);

                    return Current;
                }
            }
        }
    }
}
