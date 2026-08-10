#if UNITY_COLLECTIONS

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.Collections
{
    public class NativeReferenceExtensionsTests
    {
        [Test]
        public void DisposalMembers_HandleCreatedAndDefaultReferences()
        {
            var synchronous = new NativeReference<int>(1, AllocatorManager.Persistent);
            var scheduled = new NativeReference<int>(2, AllocatorManager.Persistent);
            var unset = new NativeReference<int>(3, AllocatorManager.Persistent);

            try
            {
                EncosyNativeReferenceExtensions.DisposeIfCreated(synchronous);
                synchronous = default;

                JobHandle handle = EncosyNativeReferenceExtensions.DisposeIfCreated(
                      scheduled
                    , default
                );
                handle.Complete();
                scheduled = default;

                EncosyNativeReferenceExtensions.DisposeIfCreated(default(NativeReference<int>));
                var unchanged = EncosyNativeReferenceExtensions.DisposeIfCreated(
                      default(NativeReference<int>)
                    , default
                );
                unchanged.Complete();

                EncosyNativeReferenceExtensions.DisposeUnset(ref unset);
                Assert.IsFalse(unset.IsCreated);
            }
            finally
            {
                EncosyNativeReferenceExtensions.DisposeIfCreated(synchronous);
                EncosyNativeReferenceExtensions.DisposeIfCreated(scheduled);
                EncosyNativeReferenceExtensions.DisposeIfCreated(unset);
            }
        }
    }
}

#endif
