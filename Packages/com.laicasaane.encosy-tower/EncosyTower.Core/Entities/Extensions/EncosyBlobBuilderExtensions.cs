#if UNITY_ENTITIES

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EncosyTower.Entities
{
    public static class EncosyBlobBuilderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlobBuilderArray<T> ConstructArray<T>(
              this BlobBuilder builder
            , ref BlobArray<T> ptr
            , NativeArray<T> src
        )
            where T : struct
        {
            var arrayBuilder = builder.Allocate(ref ptr, src.Length);
            src.AsReadOnlySpan().CopyTo(arrayBuilder.AsSpan());
            return arrayBuilder;
        }

        public static void ConstructString<T>(
              this ref BlobBuilder builder
            , ref BlobString blobStr
            , T fixedString
        )
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var arrayBuilder = builder.Allocate(
                  ref UnsafeUtility.As<BlobString, BlobArray<byte>>(ref blobStr)
                , fixedString.Length
            );

            for (var i = 0; i < fixedString.Length; i++)
            {
                arrayBuilder[i] = fixedString[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConstructString(
              this ref BlobBuilder builder
            , ref BlobString blobStr
            , ReadOnlySpan<byte> fixedString
        )
        {
            var arrayBuilder = builder.Allocate(
                  ref UnsafeUtility.As<BlobString, BlobArray<byte>>(ref blobStr)
                , fixedString.Length
            );

            fixedString.CopyTo(arrayBuilder.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>The returned span borrows the blob storage; the blob must remain alive and immutable for the span's lifetime.</safety>
        public unsafe static ReadOnlySpan<T> AsReadOnlySpan<T>(this ref BlobArray<T> self) where T : struct
        {
            // SAFETY: BlobArray.GetUnsafePtr points at the live contiguous blob storage for Length elements.
            unsafe
            {
                return new ReadOnlySpan<T>(self.GetUnsafePtr(), self.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>The returned span borrows the builder's contiguous storage; the builder must remain alive and must not relocate the array.</safety>
        public unsafe static Span<T> AsSpan<T>(this ref BlobBuilderArray<T> self) where T : struct
        {
            // SAFETY: BlobBuilderArray.GetUnsafePtr points at the live contiguous builder storage for Length elements.
            unsafe
            {
                return new Span<T>(self.GetUnsafePtr(), self.Length);
            }
        }
    }
}

#endif
