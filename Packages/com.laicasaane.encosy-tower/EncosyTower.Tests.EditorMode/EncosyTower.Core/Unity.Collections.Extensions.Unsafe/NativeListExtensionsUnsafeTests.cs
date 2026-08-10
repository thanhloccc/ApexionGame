#if UNITY_COLLECTIONS

using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    public class NativeListExtensionsUnsafeTests
    {
        [Test]
        public void ElementRefs_AliasOriginalList()
        {
            var list = new NativeList<int>(3, AllocatorManager.Persistent);

            try
            {
                list.Add(1);
                list.Add(2);
                list.Add(3);

                ref var writable = ref EncosyNativeListExtensionsUnsafe.ElementAsUnsafeRefRW(
                      list
                    , 1
                );
                writable = 20;
                ref readonly var readOnly = ref EncosyNativeListExtensionsUnsafe
                    .ElementAsUnsafeRefRO(
                          list
                        , 1
                    );

                Assert.AreEqual(20, list[1]);
                Assert.AreEqual(20, readOnly);
            }
            finally
            {
                list.Dispose();
            }
        }
    }
}

#endif
