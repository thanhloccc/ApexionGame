using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    public class NativeArrayExtensionsUnsafeTests
    {
        [Test]
        public void ElementRefs_AliasOriginalArray()
        {
            var array = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            try
            {
                ref var writable = ref EncosyNativeArrayExtensionsUnsafe.ElementAsUnsafeRefRW(
                      array
                    , 1
                );
                writable = 20;
                ref readonly var readOnly = ref EncosyNativeArrayExtensionsUnsafe
                    .ElementAsUnsafeRefRO(
                          array
                        , 1
                    );

                Assert.AreEqual(20, array[1]);
                Assert.AreEqual(20, readOnly);
            }
            finally
            {
                array.Dispose();
            }
        }

        [Test]
        public void MemoryCopyWithinArray_CopiesRequestedNonOverlappingRange()
        {
            var array = new NativeArray<int>(new[] { 1, 2, 3, 0, 0, 0 }, Allocator.Temp);

            try
            {
                EncosyNativeArrayExtensionsUnsafe.MemoryCopyUnsafe(array, 0, 3, 3);

                CollectionAssert.AreEqual(new[] { 1, 2, 3, 1, 2, 3 }, array.ToArray());
            }
            finally
            {
                array.Dispose();
            }
        }

        [Test]
        public void MemoryCopyBetweenArrays_CheckedAndUncheckedFormsCopyRequestedRange()
        {
            var source = new NativeArray<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var destination = new NativeArray<int>(4, Allocator.Temp);

            try
            {
                EncosyNativeArrayExtensionsUnsafe.MemoryCopyUnsafe(
                      source
                    , 1
                    , destination
                    , 0
                    , 2
                );

                CollectionAssert.AreEqual(new[] { 2, 3, 0, 0 }, destination.ToArray());

                destination[0] = 0;
                destination[1] = 0;

                EncosyNativeArrayExtensionsUnsafe.MemoryCopyUnsafeWithoutChecks(
                      source
                    , 0
                    , destination
                    , 2
                    , 2
                );

                CollectionAssert.AreEqual(new[] { 0, 0, 1, 2 }, destination.ToArray());
            }
            finally
            {
                destination.Dispose();
                source.Dispose();
            }
        }
    }
}
