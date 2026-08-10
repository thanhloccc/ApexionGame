using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeReferenceTests.cs against the Native forwarder.
    public partial class ReferenceNativeTests
    {
        [Test]
        public void Constructor_WithValue_StoresValue()
        {
            using var reference = new ReferenceNative<int>(42, Allocator.Temp);

            Assert.IsTrue(reference.IsCreated);
            Assert.AreEqual(42, reference.Value);
        }

        [Test]
        public void Constructor_Default_ClearsToZero()
        {
            using var reference = new ReferenceNative<int>(Allocator.Temp);

            Assert.IsTrue(reference.IsCreated);
            Assert.AreEqual(0, reference.Value);
        }

        [Test]
        public void DefaultReference_IsNotCreated()
        {
            ReferenceNative<int> reference = default;

            Assert.IsFalse(reference.IsCreated);
        }

        [Test]
        public void Value_SetGet()
        {
            var reference = new ReferenceNative<int>(Allocator.Temp);

            try
            {
                reference.Value = 99;
                Assert.AreEqual(99, reference.Value);
            }
            finally
            {
                reference.Dispose();
            }
        }

        [Test]
        public void CopyFrom_CopiesValue()
        {
            using var a = new ReferenceNative<int>(1, Allocator.Temp);
            using var b = new ReferenceNative<int>(2, Allocator.Temp);

            a.CopyFrom(b);

            Assert.AreEqual(2, a.Value);
            Assert.AreEqual(2, b.Value);
        }

        [Test]
        public void CopyTo_CopiesValue()
        {
            using var a = new ReferenceNative<int>(1, Allocator.Temp);
            using var b = new ReferenceNative<int>(2, Allocator.Temp);

            a.CopyTo(b);

            Assert.AreEqual(1, a.Value);
            Assert.AreEqual(1, b.Value);
        }

        [Test]
        public void Equals_ByValue()
        {
            using var a = new ReferenceNative<int>(7, Allocator.Temp);
            using var b = new ReferenceNative<int>(7, Allocator.Temp);
            using var c = new ReferenceNative<int>(8, Allocator.Temp);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals((object)c));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void StaticCopy_CopiesSourceIntoDestination()
        {
            using var destination = new ReferenceNative<int>(1, Allocator.Temp);
            using var source = new ReferenceNative<int>(2, Allocator.Temp);

            ReferenceNative<int>.Copy(destination, source);

            Assert.AreEqual(2, destination.Value);
            Assert.AreEqual(2, source.Value);
        }

        [Test]
        public void AsReadOnly_ReflectsValue()
        {
            using var reference = new ReferenceNative<int>(55, Allocator.Temp);

            var readOnly = reference.AsReadOnly();
            ReferenceNative<int>.ReadOnly converted = reference;

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(55, readOnly.Value);
            Assert.IsTrue(converted.IsCreated);
            Assert.AreEqual(55, converted.Value);
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var reference = new ReferenceNative<int>(1, Allocator.Temp);
            Assert.IsTrue(reference.IsCreated);

            reference.Dispose();

            Assert.IsFalse(reference.IsCreated);
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var reference = new ReferenceNative<int>(1, Allocator.TempJob);

            var handle = reference.Dispose(default);

            Assert.IsFalse(reference.IsCreated);
            Assert.DoesNotThrow(() => handle.Complete());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void UseAfterDispose_Throws()
        {
            var reference = new ReferenceNative<int>(1, Allocator.Temp);
            reference.Dispose();

            Assert.Catch(() => _ = reference.Value);
        }
    }
}
