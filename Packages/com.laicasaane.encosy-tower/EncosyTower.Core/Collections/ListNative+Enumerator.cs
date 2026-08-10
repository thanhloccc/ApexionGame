using System.Collections;
using System.Collections.Generic;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections
{
    public partial struct ListNative<T>
        where T : unmanaged
    {
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnly _list;
            private readonly int _version;
            private int _index;
            private T _current;

            internal unsafe Enumerator(ListUnsafe<T>* data, in ReadOnly list)
            {
                _list = list;
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

                if ((uint)_index < (uint)_list.Count)
                {
                    _current = _list[_index++];
                    return true;
                }
                _index = _list.Count + 1;
                _current = default;
                return false;
            }

            private readonly int CurrentVersion()
            {
                // SAFETY: The enumerator's view safety check and owner lifetime keep the header live.
                unsafe
                {
                    return _list.m_Data->_version;
                }
            }

            public readonly T Current
                => _current;

            readonly object IEnumerator.Current
                => Current;

            public void Reset()
            {
                ThrowHelper.ThrowIfCollectionWasModified(_version == CurrentVersion());

                _index = 0;
                _current = default;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
