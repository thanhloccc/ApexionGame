// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using ThrowHelper = EncosyTower.Collections.ThrowHelper;

namespace EncosyTower.Buffers
{
    public struct BufferProviderEnumerator<TProvider, TBuffer, T> : IEnumerator<T>, IEnumerator
        where TProvider : IBufferProvider<TBuffer, T>
        where TBuffer : IBuffer<T>
    {
        private readonly TProvider _provider;
        private readonly int _version;
        private int _index;
        private T _current;

        internal BufferProviderEnumerator([NotNull] TProvider provider)
        {
            _provider = provider;
            _index = 0;
            _version = provider.Version;
            _current = default;
        }

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            var localList = _provider;

            if (_version == localList.Version && ((uint)_index < (uint)localList.Count))
            {
                _current = localList.Buffer[_index];
                _index++;
                return true;

            }
            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            ThrowHelper.ThrowIfCollectionWasModified(_version == _provider.Version);

            _index = _provider.Count + 1;
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
            ThrowHelper.ThrowIfCollectionWasModified(_version == _provider.Version);

            _index = 0;
            _current = default;
        }

        readonly object IEnumerator.Current
        {
            get
            {
                ThrowHelper.ThrowIfEnumeratorOperationIsInvalid(_index != 0 && _index != _provider.Count + 1);

                return Current;
            }
        }
    }
}
