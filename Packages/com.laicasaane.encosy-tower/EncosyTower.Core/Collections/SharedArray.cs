// https://github.com/stella3d/SharedArray

// MIT License
//
// Copyright(c) 2020 Stella Cannefax
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify,
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies
// or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
// OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.CompilerServices.Exposed;
using System.Runtime.InteropServices;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    /// <summary>
    /// An array usable as both a NativeArray and managed array
    /// </summary>
    /// <typeparam name="T">The type of the array element</typeparam>
    public class SharedArray<T> : SharedArray<T, T>
        where T : unmanaged
    {
        public SharedArray(int size) : base(size)
        {
        }

        public SharedArray([NotNull] T[] source) : base(source)
        {
        }

        public SharedArray(in ArraySegment<T> source) : base(source)
        {
        }

        public SharedArray(ReadOnlySpan<T> source) : base(source)
        {
        }

        public SharedArray([NotNull] ICollection<T> source) : base(source)
        {
        }

        public SharedArray([NotNull] ICollection<T> source, int extraSize) : base(source, extraSize)
        {
        }
    }

    /// <summary>
    /// An array usable as both a native and managed array
    /// </summary>
    /// <typeparam name="T">The element type in the managed representation.</typeparam>
    /// <typeparam name="TNative">The element type in the NativeArray representation. Must be
    /// the same size as <typeparamref name="T"/>.</typeparam>
    public class SharedArray<T, TNative> : IDisposable, IClearable, IResizable, IEnumerable<T>, IIndexer<T>
        , IAsSpan<T>, IAsReadOnlySpan<T>, IAsMemory<T>, IAsReadOnlyMemory<T>
        , IAsNativeArray<TNative>, IAsNativeSlice<TNative>, IHasLength
        where T : unmanaged
        where TNative : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        private GCHandle _gcHandle;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        private AtomicSafetyHandle m_Safety;
#endif

        internal T[] _managed;
        internal NativeArray<TNative> _native;
        internal int _version;
#pragma warning restore IDE1006 // Naming Styles

        protected SharedArray()
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());
            Initialize(Array.Empty<T>());
        }

        public SharedArray(int size)
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());
            ThrowIfSizeNegative(size >= 0);
            Initialize(size == 0 ? Array.Empty<T>() : new T[size]);
        }

        public SharedArray([NotNull] T[] source)
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());
            Initialize(source);
        }

        public SharedArray(in ArraySegment<T> source) : this(source.ToArray())
        {
        }

        public SharedArray(ReadOnlySpan<T> source) : this(source.ToArray())
        {
        }

        public SharedArray(in NativeArray<TNative> source)
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());

            var managed = new T[source.Length];
            Initialize(managed);
            AsNativeArray().CopyFrom(source);
        }

        public SharedArray(in NativeSlice<TNative> source)
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());

            var managed = new T[source.Length];
            Initialize(managed);
            source.CopyTo(AsNativeArray());
        }

        public SharedArray([NotNull] ICollection<T> source)
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());

            var managed = new T[source.Count];
            source.CopyTo(managed, 0);

            Initialize(managed);
        }

        public SharedArray([NotNull] ICollection<T> source, int extraSize)
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());

            var managed = new T[source.Count + extraSize];
            source.CopyTo(managed, 0);

            Initialize(managed);
        }

        ~SharedArray()
        {
            Dispose();
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _managed.Length;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfIndexIsOutOfRange((uint)index < (uint)_managed.Length);
                return _managed[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ThrowIfIndexIsOutOfRange((uint)index < (uint)_managed.Length);
                _version++;
                _managed[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T[]([NotNull] SharedArray<T, TNative> self)
        {
            return self.AsManagedArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ArraySegment<T>([NotNull] SharedArray<T, TNative> self)
        {
            return self.AsArraySegment();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>([NotNull] SharedArray<T, TNative> self)
        {
            return self.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>([NotNull] SharedArray<T, TNative> self)
        {
            return self.AsReadOnlySpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Memory<T>([NotNull] SharedArray<T, TNative> self)
        {
            return self.AsMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyMemory<T>([NotNull] SharedArray<T, TNative> self)
        {
            return self.AsReadOnlyMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArray<TNative>([NotNull] SharedArray<T, TNative> self)
        {
            return self.AsNativeArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeSlice<TNative>([NotNull] SharedArray<T, TNative> self)
        {
            return self.AsNativeSlice();
        }

        /// <summary>
        /// Allows taking pointer of SharedArray in 'fixed' statements
        /// </summary>
        /// <returns></returns>
        public ref T GetPinnableReference()
        {
            _version++;

            if (_managed.Length > 0)
            {
                return ref _managed[0];
            }

            return ref UnsafeExposed.NullRef<T>();
        }

        /// <summary>
        /// Resize and keep the content.
        /// </summary>
        /// <param name="newSize"></param>
        public void Resize(int newSize)
            => Resize(newSize, true);

        public void Resize(int newSize, bool copyContent)
        {
            _version++;

            ThrowIfSizeNegative(newSize >= 0);

            if (newSize == _managed.Length)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
#endif

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            if (copyContent)
            {
                Array.Resize(ref _managed, newSize);
            }
            else
            {
                _managed = new T[newSize];
            }

            Initialize();
        }

        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            Array.Clear(_managed, 0, _managed.Length);
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public void Dispose()
        {
            if (_managed == null)
            {
                return;
            }

            _version++;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
#endif

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            _managed = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] AsManagedArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            return _managed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<T> AsArraySegment()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            return _managed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            return _managed.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            return _managed.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> AsMemory()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            return _managed.AsMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<T> AsReadOnlyMemory()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            return _managed.AsMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<TNative> AsNativeArray()
        {
            return _native;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TNative> AsNativeSlice()
        {
            return _native.Slice();
        }

        /// <summary>
        /// Raw pointer into the pinned managed buffer, for the shared native view family.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe TNative* GetUnsafeBufferPointer()
        {
            // SAFETY: The shared array pins its managed storage for the lifetime of the native alias.
            unsafe
            {
                return (TNative*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(_native);
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        /// <summary>
        /// The single representative safety handle, reused by the shared native view family.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal AtomicSafetyHandle GetSafetyHandle()
            => m_Safety;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool AreTypesEqualSize()
        {
            return UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<TNative>();
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        protected static void ThrowIfSizeNegative([DoesNotReturnIf(false)] bool isZeroOrPositive)
        {
            if (isZeroOrPositive == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("size must be equal or greater than 0");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfIndexIsOutOfRange([DoesNotReturnIf(false)] bool isWithinRange)
        {
            if (isWithinRange == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("index is outside the range of valid indices for the SharedArray<T>");
        }

        internal void Initialize(T[] managed)
        {
            _version++;
            _managed = managed;
            Initialize();
        }

        private void Initialize()
        {
            // Unity's default garbage collector doesn't move objects around, so pinning the array in memory
            // should not even be necessary. Better to be safe, though
            _gcHandle = GCHandle.Alloc(_managed, GCHandleType.Pinned);
            CreateNativeAlias();

            void CreateNativeAlias()
            {
                // this is the trick to making a NativeArray view of a managed array (or any pointer)
                // SAFETY: _managed is pinned by _gcHandle for the complete duration of this fixed scope.
                unsafe
                {
                    fixed (void* ptr = _managed)
                    {
                        _native = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TNative>(
                            ptr, _managed.Length, Allocator.None
                        );
                    }
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                m_Safety = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _native, m_Safety);
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly SharedArray<T, TNative> _sharedArray;
            private readonly int _version;
            private readonly int _length;
            private int _index;
            private Option<T> _current;

            public Enumerator([NotNull] SharedArray<T, TNative> sharedArray)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                // Unlike the other safety checks, only check if it's safe to read.
                // Enumerating an array of structs gives the user copies of each element, since structs pass by value.
                // This means that the source memory can't be modified while enumerating.
                AtomicSafetyHandle.CheckReadAndThrow(sharedArray.m_Safety);
#endif

                _sharedArray = sharedArray;
                _version = sharedArray._version;
                _length = sharedArray.Length;
                _index = -1;
                _current = Option.None;
            }

            public readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current.GetValueOrThrow();
            }

            public bool MoveNext()
            {
                var sharedArray = _sharedArray;
                var array = sharedArray._managed;

                if (_version == sharedArray._version && ((uint)(_index + 1) < (uint)_length))
                {
                    _index++;
                    _current = array[_index];
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                ThrowIfEnumFailedVersion(_version == _sharedArray._version);

                _index = _length + 1;
                _current = Option.None;
                return false;
            }

            void IEnumerator.Reset()
            {
                ThrowIfEnumFailedVersion(_version == _sharedArray._version);

                _index = -1;
                _current = Option.None;
            }

            readonly object IEnumerator.Current
            {
                get
                {
                    ThrowIfEnumOpCantHappen((uint)_index < (uint)_length);

                    return Current;
                }
            }

            public readonly void Dispose()
            {
            }

            [HideInCallstack, StackTraceHidden]
            [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
            [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
            [Conditional(UNITY_COLLECTIONS_CHECKS)]
            private static void ThrowIfEnumFailedVersion([DoesNotReturnIf(false)] bool validVersion)
            {
                if (validVersion == false)
                {
                    throw CreateException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                static InvalidOperationException CreateException()
                    => new("SharedArray was modified during enumeration.");
            }

            [HideInCallstack, StackTraceHidden]
            [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
            [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
            [Conditional(UNITY_COLLECTIONS_CHECKS)]
            private static void ThrowIfEnumOpCantHappen([DoesNotReturnIf(false)] bool validIndex)
            {
                if (validIndex == false)
                {
                    throw CreateException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                static InvalidOperationException CreateException()
                    => new("Invalid enumerator state: enumeration cannot proceed.");
            }
        }
    }
}
