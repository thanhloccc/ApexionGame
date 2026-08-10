#if !UNITY_COLLECTIONS

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Provides utility methods for unsafe, untyped buffers.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static class UnsafeUtilityExtensions
    {
        /// <summary>
        /// Swaps bytes between two buffers.
        /// </summary>
        /// <param name="ptr">A buffer.</param>
        /// <param name="otherPtr">Another buffer.</param>
        /// <param name="size">The number of bytes to swap.</param>
        /// <exception cref="InvalidOperationException">Thrown if the two ranges of bytes to
        /// swap overlap in memory.</exception>
        internal static unsafe void MemSwap(void* ptr, void* otherPtr, long size)
        {
            // SAFETY: The caller provides disjoint writable buffers and a bounded byte count.
            unsafe
            {
                byte* dst = (byte*) ptr;
                byte* src = (byte*) otherPtr;

                CheckMemSwapOverlap(dst, src, size);

                var tmp = stackalloc byte[1024];

                while (size > 0)
                {
                    var numBytes = math.min(size, 1024);
                    UnsafeUtility.MemCpy(tmp, dst, numBytes);
                    UnsafeUtility.MemCpy(dst, src, numBytes);
                    UnsafeUtility.MemCpy(src, tmp, numBytes);

                    size -= numBytes;
                    src += numBytes;
                    dst += numBytes;
                }
            }
        }

        /// <summary>
        /// Reads an element from a buffer after bounds checking.
        /// </summary>
        /// <typeparam name="T">The type of element.</typeparam>
        /// <param name="source">The buffer to read from.</param>
        /// <param name="index">The index of the element.</param>
        /// <param name="capacity">The buffer capacity (in number of elements). Used for the bounds checking.</param>
        /// <safety>source must reference capacity readable unmanaged elements.</safety>
        /// <returns>The element read from the buffer.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of bounds.</exception>
        public unsafe static T ReadArrayElementBoundsChecked<T>(void* source, int index, int capacity)
            where T : unmanaged
        {
            CheckIndexRange(index, capacity);

            // SAFETY: CheckIndexRange bounds the read within source capacity.
            unsafe
            {
                return UnsafeUtility.ReadArrayElement<T>(source, index);
            }
        }

        /// <summary>
        /// Writes an element to a buffer after bounds checking.
        /// </summary>
        /// <typeparam name="T">The type of element.</typeparam>
        /// <param name="destination">The buffer to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="index">The index at which to store the element.</param>
        /// <param name="capacity">The buffer capacity (in number of elements). Used for the bounds checking.</param>
        /// <safety>destination must reference capacity writable unmanaged elements.</safety>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of bounds.</exception>
        public unsafe static void WriteArrayElementBoundsChecked<T>(void* destination, int index, T value, int capacity)
            where T : unmanaged
        {
            CheckIndexRange(index, capacity);

            // SAFETY: CheckIndexRange bounds the write within destination capacity.
            unsafe
            {
                UnsafeUtility.WriteArrayElement<T>(destination, index, value);
            }
        }

        /// <summary>
        /// Returns the address of a read-only reference.
        /// </summary>
        /// <typeparam name="T">The type of referenced value.</typeparam>
        /// <param name="value">A read-only reference.</param>
        /// <returns>A pointer to the referenced value.</returns>
        /// <safety>The returned pointer is valid only while value remains alive and must not be
        /// dereferenced after its lifetime ends.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* AddressOf<T>(in T value)
            where T : unmanaged
        {
            // SAFETY: The returned pointer is borrowed from the caller's referenced value.
            unsafe
            {
                return ILSupport.AddressOf(in value);
            }
        }

        /// <summary>
        /// Returns a read-write reference from a read-only reference.
        /// <remarks>Useful when you want to pass an `in` arg (read-only reference) where a `ref`
        /// arg (read-write reference) is expected.
        /// Do not mutate the referenced value, as doing so may break the runtime's assumptions.</remarks>
        /// </summary>
        /// <typeparam name="T">The type of referenced value.</typeparam>
        /// <param name="value">A read-only reference.</param>
        /// <returns>A read-write reference to the value referenced by `item`.</returns>
        /// <safety>The caller must keep value alive and must not mutate through the returned
        /// reference when value is read-only.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T AsRef<T>(in T value)
            where T : unmanaged
        {
            // SAFETY: The caller must keep the referenced value alive and must not violate its read-only contract.
            unsafe
            {
                return ref ILSupport.AsRef(in value);
            }
        }

        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        static unsafe void CheckMemSwapOverlap(byte* dst, byte* src, long size)
        {
            // SAFETY: The pointers are used only for range comparison; no memory is dereferenced.
            unsafe
            {
                ThrowIfMemSwapBlocksOverlap(dst + size > src && src + size > dst);
            }
        }

        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        static void CheckIndexRange(int index, int capacity)
        {
            ThrowIfIndexIsOutOfRange(index > capacity - 1 || index < 0, index, capacity);
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfMemSwapBlocksOverlap([DoesNotReturnIf(true)] bool overlap)
        {
            if (overlap)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("MemSwap memory blocks overlap.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfIndexIsOutOfRange(
            [DoesNotReturnIf(true)] bool outOfRange
          , int index
          , int capacity
        )
        {
            if (outOfRange)
            {
                throw CreateException(index, capacity);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static IndexOutOfRangeException CreateException(int index, int capacity)
                => new(
                    $"Attempt to read or write from array index {index}, which is out of bounds. " +
                    $"Array capacity is {capacity}. " +
                    "This may lead to a crash, data corruption, or reading invalid data."
                );
        }
    }
}

#endif
