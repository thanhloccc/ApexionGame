#if UNITY_COLLECTIONS

using System;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    public class NativeReferenceExtensionsUnsafeTests
    {
        [Test]
        public void ValueRefsAndSpans_AliasOriginalReference()
        {
            var reference = new NativeReference<int>(1, AllocatorManager.Persistent);

            try
            {
                ref var writable = ref EncosyNativeReferenceExtensionsUnsafe.ValueAsUnsafeRefRW(
                    reference
                );
                writable = 10;
                ref readonly var readOnly = ref EncosyNativeReferenceExtensionsUnsafe
                    .ValueAsUnsafeRefRO(reference);

                Assert.AreEqual(10, reference.Value);
                Assert.AreEqual(10, readOnly);

                Span<int> span = EncosyNativeReferenceExtensionsUnsafe.AsSpan(reference);
                span[0] = 20;
                var readOnlySpan = EncosyNativeReferenceExtensionsUnsafe.AsReadOnlySpan(
                    reference
                );

                Assert.AreEqual(20, reference.Value);
                Assert.AreEqual(20, readOnlySpan[0]);
            }
            finally
            {
                reference.Dispose();
            }
        }
    }
}

#endif
