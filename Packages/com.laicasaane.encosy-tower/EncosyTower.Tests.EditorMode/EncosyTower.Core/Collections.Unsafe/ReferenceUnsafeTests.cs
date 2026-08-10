using System;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    // Adapted from Unity.Collections.Tests/NativeReferenceTests.cs against the raw Unsafe header.
    public partial class ReferenceUnsafeTests
    {
        [Test]
        public void Constructor_WithValue_StoresValue()
        {
            using var reference = new ReferenceUnsafe<int>(42, Allocator.Temp);

            Assert.IsTrue(reference.IsCreated);
            Assert.AreEqual(42, reference.Value);
        }

        [Test]
        public void Constructor_Default_ClearsToZero()
        {
            using var reference = new ReferenceUnsafe<int>(Allocator.Temp);

            Assert.IsTrue(reference.IsCreated);
            Assert.AreEqual(0, reference.Value);
        }

        [Test]
        public void DefaultReference_IsNotCreated()
        {
            ReferenceUnsafe<int> reference = default;

            Assert.IsFalse(reference.IsCreated);
        }

        [Test]
        public void Value_SetGet()
        {
            var reference = new ReferenceUnsafe<int>(Allocator.Temp);

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
        public void ValueAsRef_Mutates()
        {
            var reference = new ReferenceUnsafe<int>(5, Allocator.Temp);

            try
            {
                ref var slot = ref reference.ValueAsRef;
                slot = 123;
                Assert.AreEqual(123, reference.Value);
            }
            finally
            {
                reference.Dispose();
            }
        }

        [Test]
        public void CopyFrom_CopiesValue()
        {
            using var a = new ReferenceUnsafe<int>(1, Allocator.Temp);
            using var b = new ReferenceUnsafe<int>(2, Allocator.Temp);

            a.CopyFrom(b);

            Assert.AreEqual(2, a.Value);
            Assert.AreEqual(2, b.Value);
        }

        [Test]
        public void CopyTo_CopiesValue()
        {
            using var a = new ReferenceUnsafe<int>(1, Allocator.Temp);
            using var b = new ReferenceUnsafe<int>(2, Allocator.Temp);

            a.CopyTo(b);

            Assert.AreEqual(1, a.Value);
            Assert.AreEqual(1, b.Value);
        }

        [Test]
        public void Equals_ByValue()
        {
            using var a = new ReferenceUnsafe<int>(7, Allocator.Temp);
            using var b = new ReferenceUnsafe<int>(7, Allocator.Temp);
            using var c = new ReferenceUnsafe<int>(8, Allocator.Temp);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals((object)c));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void IndexerLengthSpansPointerReadOnlyAndConversion_AliasOwnedValue()
        {
            using var reference = new ReferenceUnsafe<int>(5, Allocator.Temp);

            Assert.AreEqual(1, reference.Length);
            Assert.AreEqual(5, reference[0]);

            reference[0] = 6;
            var span = reference.AsSpan();
            var readOnlySpan = reference.AsReadOnlySpan();
            var readOnly = reference.AsReadOnly();
            ReferenceUnsafe<int>.ReadOnly converted = reference;

            Assert.AreEqual(6, span[0]);
            Assert.AreEqual(6, readOnlySpan[0]);
            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(6, readOnly.Value);
            Assert.IsTrue(converted.IsCreated);
            Assert.AreEqual(6, converted.Value);

            // SAFETY: The pointer aliases one live int allocation and does not escape this scope.
            unsafe
            {
                var pointer = (int*)reference.GetUnsafePtr();

                Assert.AreNotEqual(IntPtr.Zero, (IntPtr)pointer);

                *pointer = 7;
            }

            Assert.AreEqual(7, reference.Value);
            Assert.IsFalse(default(ReferenceUnsafe<int>.ReadOnly).IsCreated);
        }

        [Test]
        public void SpanCopyFamilies_AllOverloadsCopyOrReturnFalse()
        {
            using var reference = new ReferenceUnsafe<int>(0, Allocator.Temp);

            reference.CopyFrom(new[] { 1 });
            Assert.AreEqual(1, reference.Value);
            reference.CopyFrom(new[] { 2, 3 }, 1);
            Assert.AreEqual(2, reference.Value);
            reference.CopyFrom(0, new[] { 3 });
            Assert.AreEqual(3, reference.Value);
            reference.CopyFrom(0, new[] { 4, 5 }, 1);
            Assert.AreEqual(4, reference.Value);

            Assert.IsTrue(reference.TryCopyFrom(new[] { 5 }));
            Assert.IsTrue(reference.TryCopyFrom(new[] { 6, 7 }, 1));
            Assert.IsTrue(reference.TryCopyFrom(0, new[] { 7 }));
            Assert.IsTrue(reference.TryCopyFrom(0, new[] { 8, 9 }, 1));
            Assert.IsFalse(reference.TryCopyFrom(new[] { 10, 11 }));
            Assert.IsFalse(reference.TryCopyFrom(1, new[] { 10 }));
            Assert.AreEqual(8, reference.Value);

            var all = new int[1];
            var explicitLength = new int[1];
            var sourceIndex = new int[1];
            var explicitWindow = new int[1];

            reference.CopyTo(all);
            reference.CopyTo(explicitLength, 1);
            reference.CopyTo(0, sourceIndex);
            reference.CopyTo(0, explicitWindow, 1);

            Assert.AreEqual(8, all[0]);
            Assert.AreEqual(8, explicitLength[0]);
            Assert.AreEqual(8, sourceIndex[0]);
            Assert.AreEqual(8, explicitWindow[0]);
            Assert.IsTrue(reference.TryCopyTo(new int[1]));
            Assert.IsTrue(reference.TryCopyTo(new int[1], 1));
            Assert.IsTrue(reference.TryCopyTo(0, new int[1]));
            Assert.IsTrue(reference.TryCopyTo(0, new int[1], 1));
            Assert.IsFalse(reference.TryCopyTo(new int[1], 2));
            Assert.IsFalse(reference.TryCopyTo(1, new int[1]));
        }

        [Test]
        public void StaticCopy_CopiesSourceIntoDestination()
        {
            using var destination = new ReferenceUnsafe<int>(1, Allocator.Temp);
            using var source = new ReferenceUnsafe<int>(2, Allocator.Temp);

            ReferenceUnsafe<int>.Copy(destination, source);

            Assert.AreEqual(2, destination.Value);
            Assert.AreEqual(2, source.Value);
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var reference = new ReferenceUnsafe<int>(1, Allocator.Temp);
            Assert.IsTrue(reference.IsCreated);

            reference.Dispose();

            Assert.IsFalse(reference.IsCreated);
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var reference = new ReferenceUnsafe<int>(1, Allocator.TempJob);

            var handle = reference.Dispose(default);

            Assert.IsFalse(reference.IsCreated);
            Assert.DoesNotThrow(() => handle.Complete());
        }
    }
}
