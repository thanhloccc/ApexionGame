using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.Internals;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeSliceReadOnlyDebugView<>))]
    public struct NativeSliceReadOnly<T> : IEnumerable<T>, IEquatable<NativeSliceReadOnly<T>>
        , IToArray<T>, IReadOnlyIndexer<T>, IHasLength
        where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* _buffer;

        internal int _stride;
        internal int _length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal int _minIndex;
        internal int _maxIndex;

#pragma warning disable IDE1006 // Naming Styles
        internal AtomicSafetyHandle m_Safety;
#pragma warning restore IDE1006 // Naming Styles
#endif

        /// <safety>The slice must remain valid and index must be within its logical length.</safety>
        public readonly unsafe T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

                // SAFETY: The read safety handle was checked and the caller supplies an index within the slice.
                unsafe
                {
                    return UnsafeUtility.ReadArrayElementWithStride<T>(_buffer, index, _stride);
                }
            }
        }

        public readonly int Stride
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _stride;
        }

        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        /// <summary>
        /// Creates a read-only slice over raw memory, borrowing an external safety handle
        /// instead of creating or copying one. The caller is responsible for passing a
        /// handle that guards the lifetime of <paramref name="buffer"/>.
        /// </summary>
        internal unsafe NativeSliceReadOnly(
              void* buffer
            , int stride
            , int length
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            , AtomicSafetyHandle safety
#endif
        )
        {
            // SAFETY: The caller supplies a borrowed buffer guarded by the provided safety handle.
            unsafe
            {
                _buffer = (byte*)buffer;
            }
            _stride = stride;
            _length = length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            _minIndex = 0;
            _maxIndex = length - 1;
            m_Safety = safety;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceReadOnly(NativeSlice<T> slice)
            : this(slice, 0, slice.Length)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceReadOnly(NativeSlice<T> slice, int start)
            : this(slice, start, slice.Length - start)
        {
        }

        public NativeSliceReadOnly(NativeSlice<T> slice, int start, int length)
        {
            // SAFETY: The accessor exposes Unity's live NativeSlice buffer and the range checks below bound the view.
            unsafe
            {
                var accessor = new NativeSliceAccessor<T>(slice);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                ThrowIfSliceStartIsNegative(start >= 0, start);
                ThrowIfSliceLengthIsNegative(length >= 0, length);
                ThrowIfSliceRangeExceedsLength(
                      IsSliceRangeWithinLength(slice.Length, start, length)
                    , slice.Length
                    , start
                    , length
                    , nameof(slice)
                );
                ThrowIfSliceOnRestrictedRange(
                      IsSliceRangeUnrestricted(
                          accessor.MinIndex, accessor.MaxIndex, accessor.Length, start, length
                      )
                    , nameof(slice)
                );
#endif

                _stride = accessor.Stride;
                _buffer = accessor.Buffer + _stride * start;
                _length = length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                _minIndex = 0;
                _maxIndex = length - 1;
                m_Safety = accessor.Safety;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceReadOnly(NativeSliceReadOnly<T> slice)
            : this(slice, 0, slice.Length)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceReadOnly(NativeSliceReadOnly<T> slice, int start)
            : this(slice, start, slice.Length - start)
        {
        }

        public NativeSliceReadOnly(NativeSliceReadOnly<T> slice, int start, int length)
        {
            // SAFETY: The source slice owns the backing allocation and the range checks bound the borrowed view.
            unsafe
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                ThrowIfSliceStartIsNegative(start >= 0, start);
                ThrowIfSliceLengthIsNegative(length >= 0, length);
                ThrowIfSliceRangeExceedsLength(
                      IsSliceRangeWithinLength(slice.Length, start, length)
                    , slice.Length
                    , start
                    , length
                    , nameof(slice)
                );
                ThrowIfSliceOnRestrictedRange(
                      IsSliceRangeUnrestricted(
                          slice._minIndex, slice._maxIndex, slice.Length, start, length
                      )
                    , nameof(slice)
                );
#endif

                _stride = slice.Stride;
                _buffer = slice._buffer + _stride * start;
                _length = length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                _minIndex = 0;
                _maxIndex = length - 1;
                m_Safety = slice.m_Safety;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceReadOnly(NativeArray<T> array)
            : this(array, 0, array.Length)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceReadOnly(NativeArray<T> array, int start)
            : this(array, start, array.Length - start)
        {
        }

        public NativeSliceReadOnly(NativeArray<T> array, int start, int length)
        {
            // SAFETY: The accessor exposes Unity's live NativeArray buffer and the range checks bound the view.
            unsafe
            {
                var accessor = new NativeArrayAccessor<T>(array);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                ThrowIfSliceStartIsNegative(start >= 0, start);
                ThrowIfSliceLengthIsNegative(length >= 0, length);
                ThrowIfSliceRangeExceedsLength(
                      IsSliceRangeWithinLength(array.Length, start, length)
                    , array.Length
                    , start
                    , length
                    , nameof(array)
                );
                ThrowIfSliceOnRestrictedRange(
                      IsSliceRangeUnrestricted(
                          accessor.MinIndex, accessor.MaxIndex, accessor.Length, start, length
                      )
                    , nameof(array)
                );
                ThrowIfSliceIntegerOverflow(IsSliceRangeWithoutOverflow(start, length));
#endif

                _stride = UnsafeUtility.SizeOf<T>();
                byte* buffer = (byte*)accessor.Buffer + _stride * start;
                _buffer = buffer;
                _length = length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                _minIndex = 0;
                _maxIndex = length - 1;
                m_Safety = accessor.Safety;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceReadOnly(NativeArray<T>.ReadOnly array)
            : this(array, 0, array.Length)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceReadOnly(NativeArray<T>.ReadOnly array, int start)
            : this(array, start, array.Length - start)
        {
        }

        public NativeSliceReadOnly(NativeArray<T>.ReadOnly array, int start, int length)
        {
            // SAFETY: The accessor exposes Unity's live read-only array buffer and the range checks bound the view.
            unsafe
            {
                var accessor = new NativeArrayReadOnlyAccessor<T>(array);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                ThrowIfSliceStartIsNegative(start >= 0, start);
                ThrowIfSliceLengthIsNegative(length >= 0, length);
                ThrowIfSliceRangeExceedsLength(
                      IsSliceRangeWithinLength(array.Length, start, length)
                    , array.Length
                    , start
                    , length
                    , nameof(array)
                );
                ThrowIfSliceOnRestrictedRange(
                      IsReadOnlyArraySliceRangeUnrestricted(accessor.Length, start, length)
                    , nameof(array)
                );
                ThrowIfSliceIntegerOverflow(IsSliceRangeWithoutOverflow(start, length));
#endif

                _stride = UnsafeUtility.SizeOf<T>();
                byte* buffer = (byte*)accessor.Buffer + _stride * start;
                _buffer = buffer;
                _length = length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                _minIndex = 0;
                _maxIndex = length - 1;
                m_Safety = accessor.Safety;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeSliceReadOnly<T>(NativeSlice<T> slice)
            => new(slice);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeSliceReadOnly<T>(NativeArray<T> array)
            => new(array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeSliceReadOnly<T>(NativeArray<T>.ReadOnly array)
            => new(array);

        /// <summary>
        /// Reinterprets a NativeSliceReadOnly with a different data type (type punning).
        /// </summary>
        /// <typeparam name="U">The target data type.</typeparam>
        /// <returns>
        /// A new NativeSliceReadOnly that views the same memory, but is reinterpreted as the target type.
        /// </returns>
        public readonly NativeSliceReadOnly<U> SliceConvert<U>()
            where U : struct
        {
            // SAFETY: The conversion preserves the source allocation and validates size/stride
            // before returning the view.
            unsafe
            {
                var sizeofU = UnsafeUtility.SizeOf<U>();

                NativeSliceReadOnly<U> outputSlice;
                outputSlice._buffer = _buffer;
                outputSlice._stride = sizeofU;
                outputSlice._length = (_length * _stride) / sizeofU;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                ThrowIfSliceConvertStrideMismatch(IsSliceConvertStrideValid());
                ThrowIfSliceConvertOnRestrictedRange(IsSliceConvertRangeUnrestricted());
                ThrowIfSliceConvertSizeMismatch(IsSliceConvertSizeValid(sizeofU));

                outputSlice._minIndex = 0;
                outputSlice._maxIndex = outputSlice._length - 1;
                outputSlice.m_Safety = m_Safety;
#endif

                return outputSlice;
            }
        }

        public readonly NativeSliceReadOnly<U> SliceWithStride<U>(int offset)
            where U : struct
        {
            // SAFETY: The offset and element size are validated against the source stride before returning the view.
            unsafe
            {
                NativeSliceReadOnly<U> outputSlice;
                outputSlice._buffer = _buffer + offset;
                outputSlice._stride = _stride;
                outputSlice._length = _length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                ThrowIfSliceWithStrideOffsetIsOutOfRange(offset >= 0);
                ThrowIfSliceWithStrideOffsetAndSizeExceeded(
                    IsSliceWithStrideOffsetAndSizeValid<U>(offset)
                );

                outputSlice._minIndex = _minIndex;
                outputSlice._maxIndex = _maxIndex;
                outputSlice.m_Safety = m_Safety;
#endif

                return outputSlice;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly NativeSliceReadOnly<U> SliceWithStride<U>()
            where U : struct
        {
            return SliceWithStride<U>(0);
        }

        public readonly void CopyTo(NativeArray<T> array)
        {
            ThrowIfArrayLengthMismatch(Length == array.Length, array.Length, Length);

            // SAFETY: Length equality and the slice's read safety handle bound both copy ranges.
            unsafe
            {
                var sizeOf = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemCpyStride(
                      array.GetUnsafePtr()
                    , sizeOf
                    , this.GetUnsafeReadOnlyPtr()
                    , Stride
                    , sizeOf
                    , _length
                );
            }
        }

        public readonly void CopyTo(T[] array)
        {
            ThrowIfArrayLengthMismatch(Length == array.Length, array.Length, Length);

            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            IntPtr addr = handle.AddrOfPinnedObject();

            // SAFETY: The pinned destination and validated slice length bound both copy ranges.
            unsafe
            {
                var sizeOf = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemCpyStride((byte*)addr, sizeOf, this.GetUnsafeReadOnlyPtr(), Stride, sizeOf, _length);
            }

            handle.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray()
        {
            T[] array = new T[Length];
            CopyTo(array);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsSpan()
        {
            // SAFETY: The returned span borrows the slice allocation and is bounded by Length.
            unsafe
            {
                return new(this.GetUnsafeReadOnlyPtr(), Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(ref this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(NativeSliceReadOnly<T> other)
        {
            // SAFETY: Pointer comparison does not dereference memory.
            unsafe
            {
                return _buffer == other._buffer && _stride == other._stride && _length == other._length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object obj)
            => obj is NativeSliceReadOnly<T> other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode()
        {
            // SAFETY: Pointer conversion is used only for identity hashing; no memory is dereferenced.
            unsafe
            {
                return HashValue.Combine((int)_buffer, _stride, _length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(NativeSliceReadOnly<T> left, NativeSliceReadOnly<T> right)
            => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(NativeSliceReadOnly<T> left, NativeSliceReadOnly<T> right)
            => !left.Equals(right);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSliceRangeWithinLength(int sourceLength, int start, int length)
            => start + length <= sourceLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSliceRangeUnrestricted(
              int minIndex
            , int maxIndex
            , int sourceLength
            , int start
            , int length
        )
            => ((minIndex != 0 || maxIndex != sourceLength - 1)
                && (start < minIndex || maxIndex < start || maxIndex < start + length - 1)
            ) == false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsReadOnlyArraySliceRangeUnrestricted(
              int sourceLength
            , int start
            , int length
        )
            => (start < 0
                || sourceLength - 1 < start
                || sourceLength - 1 < start + length - 1
            ) == false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSliceRangeWithoutOverflow(int start, int length)
            => start + length >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool IsSliceConvertStrideValid()
            => _stride == UnsafeUtility.SizeOf<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool IsSliceConvertRangeUnrestricted()
            => _minIndex == 0 && _maxIndex == _length - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool IsSliceConvertSizeValid(int sizeOfTarget)
            => _stride * _length % sizeOfTarget == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSliceWithStrideOffsetAndSizeValid<U>(int offset)
            where U : struct
            => offset + UnsafeUtility.SizeOf<U>() <= UnsafeUtility.SizeOf<T>();
#endif

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfArrayLengthMismatch(
              [DoesNotReturnIf(false)] bool valid
            , int arrayLength
            , int length
        )
        {
            if (valid == false)
            {
                throw CreateException(arrayLength, length);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentException CreateException(int arrayLength, int length)
                => new($"array.Length ({arrayLength}) does not match the Length of this instance ({length}).", "array");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceWithStrideOffsetAndSizeExceeded(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentException CreateException()
                => new("SliceWithStride sizeof(U) + offset must be <= sizeof(T)", "offset");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceWithStrideOffsetIsOutOfRange(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException()
                => new("offset", "SliceWithStride offset must be >= 0");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceConvertSizeMismatch(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("SliceConvert requires that Length * sizeof(T) is a multiple of sizeof(U).");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceConvertOnRestrictedRange(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("SliceConvert may not be used on a restricted range array");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceConvertStrideMismatch(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("SliceConvert requires that stride matches the size of the source type");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceOnRestrictedRange(
              [DoesNotReturnIf(false)] bool valid
            , string paramName
        )
        {
            if (valid == false)
            {
                throw CreateException(paramName);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentException CreateException(string paramName)
                => new($"Slice may not be used on a restricted range {paramName}", paramName);
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceRangeExceedsLength(
              [DoesNotReturnIf(false)] bool valid
            , int sourceLength
            , int start
            , int length
            , string paramName
        )
        {
            if (valid == false)
            {
                throw CreateException(sourceLength, start, length, paramName);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentException CreateException(
                  int sourceLength
                , int start
                , int length
                , string paramName
            )
                => new(
                    $"Slice start + length ({start + length}) range must be <= " +
                    $"{paramName}.Length ({sourceLength})"
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceLengthIsNegative(
              [DoesNotReturnIf(false)] bool valid
            , int length
        )
        {
            if (valid == false)
            {
                throw CreateException(length);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException(int length)
                => new("length", $"Slice length {length} < 0.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceStartIsNegative(
              [DoesNotReturnIf(false)] bool valid
            , int start
        )
        {
            if (valid == false)
            {
                throw CreateException(start);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException(int start)
                => new("start", $"Slice start {start} < 0.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSliceIntegerOverflow(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentException CreateException()
                => new("Slice start + length ({start + length}) causes an integer overflow");
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private readonly NativeSliceReadOnly<T> _slice;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(ref NativeSliceReadOnly<T> slice)
            {
                _slice = slice;
                _index = -1;
            }

            public readonly T Current
                => _slice[_index];

            readonly object IEnumerator.Current
                => Current;

            public bool MoveNext()
            {
                _index++;
                return _index < _slice.Length;
            }

            public void Reset()
            {
                _index = -1;
            }

            public readonly void Dispose()
            {
            }
        }
    }

    internal sealed class NativeSliceReadOnlyDebugView<T>
        where T : struct
    {
        private NativeSliceReadOnly<T> _slice;

        public T[] Items
            => _slice.ToArray();

        public NativeSliceReadOnlyDebugView(NativeSliceReadOnly<T> slice)
        {
            _slice = slice;
        }
    }
}
