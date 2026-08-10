// Adapted from Unity.Collections.Tests/NativeReferenceTests.cs.

using System;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedReferenceTests
    {
        [Test]
        public void Constructors_StoreDefaultAndProvidedValues()
        {
            using var defaultReference = new SharedReference<int>();
            using var valueReference = new SharedReference<int>(42);

            Assert.AreEqual(1, defaultReference.Length);
            Assert.AreEqual(0, defaultReference.ValueRO);
            Assert.AreEqual(1, valueReference.Length);
            Assert.AreEqual(42, valueReference.ValueRO);
        }

        [Test]
        public void ValueRW_WriteIsVisibleThroughValueRO()
        {
            using var reference = new SharedReference<int>(1);

            reference.ValueRW = 99;

            Assert.AreEqual(99, reference.ValueRO);
        }

        [Test]
        public void Views_ReflectContentAndSpanWrite()
        {
            using var reference = new SharedReference<int>(42);

            var span = reference.AsSpan();
            var readOnlySpan = reference.AsReadOnlySpan();
            var managed = reference.AsManagedArray();
            var memory = reference.AsMemory();
            var readOnlyMemory = reference.AsReadOnlyMemory();
            var nativeSpan = reference.AsSpanNative();
            var readOnlyNativeSpan = reference.AsReadOnlySpanNative();
            var nativeArray = reference.AsNativeArray();
            var nativeSlice = reference.AsNativeSlice();

            Assert.AreEqual(1, span.Length);
            Assert.AreEqual(42, span[0]);
            Assert.AreEqual(1, readOnlySpan.Length);
            Assert.AreEqual(42, readOnlySpan[0]);
            Assert.AreEqual(42, managed[0]);
            Assert.AreEqual(42, memory.Span[0]);
            Assert.AreEqual(42, readOnlyMemory.Span[0]);
            Assert.AreEqual(1, nativeSpan.Length);
            Assert.AreEqual(42, nativeSpan[0]);
            Assert.AreEqual(1, readOnlyNativeSpan.Length);
            Assert.AreEqual(42, readOnlyNativeSpan[0]);
            Assert.AreEqual(1, nativeArray.Length);
            Assert.AreEqual(42, nativeArray[0]);
            Assert.AreEqual(1, nativeSlice.Length);
            Assert.AreEqual(42, nativeSlice[0]);

            span[0] = 99;

            Assert.AreEqual(99, reference.ValueRO);
        }

        [Test]
        public void ImplicitAndExplicitOperators_ReflectContent()
        {
            using var reference = new SharedReference<int, uint>(42);

            Span<int> span = reference;
            ReadOnlySpan<int> readOnlySpan = reference;
            Memory<int> memory = reference;
            ReadOnlyMemory<int> readOnlyMemory = reference;
            Span<uint> nativeSpan = reference;
            ReadOnlySpan<uint> readOnlyNativeSpan = reference;
            NativeArray<uint> nativeArray = reference;
            NativeSlice<uint> nativeSlice = reference;
            var managed = (int[])reference;

            Assert.AreEqual(42, span[0]);
            Assert.AreEqual(42, readOnlySpan[0]);
            Assert.AreEqual(42, memory.Span[0]);
            Assert.AreEqual(42, readOnlyMemory.Span[0]);
            Assert.AreEqual(42u, nativeSpan[0]);
            Assert.AreEqual(42u, readOnlyNativeSpan[0]);
            Assert.AreEqual(42u, nativeArray[0]);
            Assert.AreEqual(42u, nativeSlice[0]);
            Assert.AreEqual(42, managed[0]);
        }

        [Test]
        public void Dispose_CanBeCalledTwice()
        {
            var reference = new SharedReference<int>(42);

            Assert.DoesNotThrow(reference.Dispose);
            Assert.DoesNotThrow(reference.Dispose);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void ValueRO_AfterDispose_Throws()
        {
            var reference = new SharedReference<int>(42);
            reference.Dispose();

            Assert.Catch(() => _ = reference.ValueRO);
        }
    }
}
