using EncosyTower.Buffers;
using EncosyTower.Collections;
using EncosyTower.Tests.Core.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Buffers
{
    public partial class BufferContractTests
    {
        [Test]
        public void IAlloc_DispatchesAllocThroughExactConstraint()
        {
            var buffer = new BufferManaged<int>(1);

            ExerciseAlloc(ref buffer);

            Assert.IsTrue(buffer.IsCreated);
            Assert.AreEqual(3, buffer.Capacity);
        }

        [Test]
        public void IBuffer_DispatchesEveryContractMemberThroughExactConstraint()
        {
            var buffer = new BufferManaged<int>(1);

            ExerciseBuffer(ref buffer);

            Assert.IsTrue(buffer.IsCreated);
            Assert.AreEqual(4, buffer.Capacity);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, buffer.AsSpan().ToArray());
        }

        [Test]
        public void IReadOnlyBuffer_DispatchesEveryContractMemberThroughExactConstraint()
        {
            var owner = new BufferManaged<int>(new[] { 1, 2, 3, 4 });
            BufferManaged<int>.ReadOnly readOnly = owner;

            ExerciseReadOnlyBuffer(readOnly);
        }

        [Test]
        public void IBufferProvider_DispatchesRefPropertiesThroughExactConstraint()
        {
            var provider = new BufferProvider<int>();

            ExerciseProvider<BufferProvider<int>, BufferManaged<int>, int>(provider);

            Assert.AreEqual(17, provider.Buffer[0]);
            Assert.AreEqual(1, provider.Count);
            Assert.AreEqual(1, provider.Version);
        }

        [Test]
        public void BufferProviderEnumerator_DirectMembersTrackProvider()
        {
            var provider = new BufferProvider<int>();
            var proxy = new ListProxy<BufferProvider<int>, BufferManaged<int>, int>(provider);
            proxy.Add(10);
            proxy.Add(20);

            var enumerator = proxy.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(10, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(20, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());

            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(10, enumerator.Current);
            enumerator.Dispose();
        }

        private static void ExerciseAlloc<TBuffer>(ref TBuffer buffer)
            where TBuffer : IAlloc
        {
            buffer.Alloc(3, new AllocatorStrategy(Allocator.Temp), memClear: true);
        }

        private static void ExerciseBuffer<TBuffer>(ref TBuffer buffer)
            where TBuffer : IBuffer<int>
        {
            buffer.Alloc(4, new AllocatorStrategy(Allocator.Temp), memClear: true);
            Assert.IsTrue(buffer.IsCreated);
            Assert.AreEqual(4, buffer.Capacity);

            buffer[0] = 1;
            Assert.AreEqual(1, buffer[0]);

            buffer.CopyFrom(new[] { 1, 2, 3, 4 });
            buffer.CopyFrom(new[] { 5, 6, 7, 8 }, 2);
            buffer.CopyFrom(1, new[] { 9, 10, 11 });
            buffer.CopyFrom(1, new[] { 12, 13, 14 }, 2);

            Assert.IsTrue(buffer.TryCopyFrom(new[] { 1, 2, 3, 4 }));
            Assert.IsTrue(buffer.TryCopyFrom(new[] { 5, 6 }, 2));
            Assert.IsTrue(buffer.TryCopyFrom(1, new[] { 7, 8 }));
            Assert.IsTrue(buffer.TryCopyFrom(1, new[] { 9, 10 }, 2));

            var destination = new int[4];
            buffer.CopyTo(destination);
            buffer.CopyTo(destination, 2);
            buffer.CopyTo(1, new int[3]);
            buffer.CopyTo(1, destination, 2);

            Assert.IsTrue(buffer.TryCopyTo(destination));
            Assert.IsTrue(buffer.TryCopyTo(destination, 2));
            Assert.IsTrue(buffer.TryCopyTo(1, new int[3]));
            Assert.IsTrue(buffer.TryCopyTo(1, destination, 2));

            Assert.AreEqual(4, buffer.AsSpan().Length);
            Assert.AreEqual(4, buffer.AsReadOnlySpan().Length);

            buffer.Resize(5);
            buffer.Resize(4, copyContent: true);
            buffer.Resize(4, copyContent: true, memClear: true);
            buffer.FastClear();
            buffer.Clear();
            buffer.Dispose();
        }

        private static void ExerciseReadOnlyBuffer<TBuffer>(TBuffer buffer)
            where TBuffer : IReadOnlyBuffer<int>
        {
            Assert.IsTrue(buffer.IsCreated);
            Assert.AreEqual(4, buffer.Capacity);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(4, buffer.AsReadOnlySpan().Length);

            var destination = new int[4];
            buffer.CopyTo(destination);
            buffer.CopyTo(destination, 2);
            buffer.CopyTo(1, new int[3]);
            buffer.CopyTo(1, destination, 2);

            Assert.IsTrue(buffer.TryCopyTo(destination));
            Assert.IsTrue(buffer.TryCopyTo(destination, 2));
            Assert.IsTrue(buffer.TryCopyTo(1, new int[3]));
            Assert.IsTrue(buffer.TryCopyTo(1, destination, 2));
        }

        private static void ExerciseProvider<TProvider, TBuffer, T>(TProvider provider)
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
        {
            ref var buffer = ref provider.Buffer;
            ref var count = ref provider.Count;
            ref var version = ref provider.Version;

            buffer[0] = (T)(object)17;
            count = 1;
            version = 1;
        }
    }
}
