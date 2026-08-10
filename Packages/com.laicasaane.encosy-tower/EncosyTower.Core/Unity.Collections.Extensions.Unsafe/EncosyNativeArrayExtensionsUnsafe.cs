using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Unsafe
{
    public static class EncosyNativeArrayExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native array alive and provide an index within its bounds.</safety>
        public static unsafe ref T ElementAsUnsafeRefRW<T>(this NativeArray<T> array, int index)
            where T : struct
        {
            // SAFETY: Caller owns the array lifetime and supplies a valid element index.
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native array alive and provide an index within its bounds.</safety>
        public static unsafe ref readonly T ElementAsUnsafeRefRO<T>(this NativeArray<T> array, int index)
            where T : struct
        {
            // SAFETY: Caller owns the array lifetime and supplies a valid element index.
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
            }
        }

        /// <safety>sourceIndex, destinationIndex, and length must describe valid ranges within
        /// the live native array.</safety>
        public static unsafe void MemoryCopyUnsafe<T>(
              this NativeArray<T> array
            , int sourceIndex
            , int destinationIndex
            , int length
        )
            where T : struct
        {
            // SAFETY: Caller supplies valid source/destination ranges within the live native array.
            unsafe
            {
                var sizeOf = UnsafeUtility.SizeOf<T>();
                var ptr = (IntPtr)array.GetUnsafePtr();

                Buffer.MemoryCopy(
                      (void*)(ptr + sourceIndex * sizeOf)
                    , (void*)(ptr + destinationIndex * sizeOf)
                    , (long)(array.Length - destinationIndex) * sizeOf
                    , (long)length * sizeOf
                );
            }
        }

        /// <safety>Both arrays must be live and the source/destination ranges must be valid for
        /// length elements.</safety>
        public static unsafe void MemoryCopyUnsafe<T>(
              this NativeArray<T> source
            , int sourceIndex
            , NativeArray<T> destination
            , int destinationIndex
            , int length
        )
            where T : struct
        {
            // SAFETY: Caller supplies valid source/destination ranges within the live native arrays.
            unsafe
            {
                var sizeOfT = UnsafeUtility.SizeOf<T>();
                var dstPtr = (IntPtr)destination.GetUnsafePtr();
                var srcPtr = (IntPtr)source.GetUnsafePtr();

                UnsafeUtility.MemCpy(
                      destination: (void*)(dstPtr + destinationIndex * sizeOfT)
                    , source: (void*)(srcPtr + sourceIndex * sizeOfT)
                    , size: length * sizeOfT
                );
            }
        }

        /// <safety>Both arrays must be live and the source/destination ranges must be valid;
        /// this overload skips Unity checks.</safety>
        public static unsafe void MemoryCopyUnsafeWithoutChecks<T>(
              this NativeArray<T> source
            , int sourceIndex
            , NativeArray<T> destination
            , int destinationIndex
            , int length
        )
            where T : struct
        {
            // SAFETY: This intentionally skips Unity checks; caller must provide valid ranges and live arrays.
            unsafe
            {
                var sizeOfT = UnsafeUtility.SizeOf<T>();
                var srcPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(source);
                var dstPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(destination);

                UnsafeUtility.MemCpy(
                      destination: (void*)(dstPtr + destinationIndex * sizeOfT)
                    , source: (void*)(srcPtr + sourceIndex * sizeOfT)
                    , size: length * sizeOfT
                );
            }
        }
    }
}
