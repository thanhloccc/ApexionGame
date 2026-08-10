using System;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public class CopyFromSpanTests
    {
        [Test]
        public void ConstructionConversionsLengthAndSlices_ExposeExpectedWindows()
        {
            var values = new[] { 1, 2, 3, 4 };
            var direct = new CopyFromSpan<int>(values.AsSpan());
            CopyFromSpan<int> converted = values.AsSpan();
            Span<int> span = converted;

            Assert.AreEqual(4, direct.Length);
            Assert.AreEqual(4, converted.Length);
            Assert.AreEqual(3, direct.Slice(1).Length);
            Assert.AreEqual(2, direct.Slice(1, 2).Length);
            Assert.AreEqual(3, span[2]);
            Assert.IsTrue(direct.TrySlice(4, out var empty));
            Assert.AreEqual(0, empty.Length);
            Assert.IsTrue(direct.TrySlice(1, 3, out var range));
            Assert.AreEqual(3, range.Length);
        }

        [Test]
        public void InvalidSlices_ThrowOrReturnFalseWithoutChangingFallbackWindow()
        {
            var helper = new CopyFromSpan<int>(new int[4]);

            Assert.Throws<ArgumentOutOfRangeException>(SliceFromInvalidStart);
            Assert.Throws<ArgumentOutOfRangeException>(SliceFromInvalidRange);
            Assert.IsFalse(helper.TrySlice(-1, out var negative));
            Assert.AreEqual(helper.Length, negative.Length);
            Assert.IsFalse(helper.TrySlice(5, out var pastEnd));
            Assert.AreEqual(helper.Length, pastEnd.Length);
            Assert.IsFalse(helper.TrySlice(3, 2, out var invalidRange));
            Assert.AreEqual(helper.Length, invalidRange.Length);
        }

        [Test]
        public void CopyFrom_AllOverloadsCopyRequestedWindows()
        {
            var destination = new int[6];
            var helper = new CopyFromSpan<int>(destination);

            helper.CopyFrom(new[] { 1, 2, 3 });
            helper.CopyFrom(new[] { 4, 5, 6 }, 2);
            helper.CopyFrom(2, new[] { 7, 8 });
            helper.CopyFrom(4, new[] { 9, 10, 11 }, 2);

            CollectionAssert.AreEqual(new[] { 4, 5, 7, 8, 9, 10 }, destination);
        }

        [Test]
        public void TryCopyFrom_AllOverloadsReturnExpectedStatusAndDoNotPartiallyWrite()
        {
            var destination = new int[6];
            var helper = new CopyFromSpan<int>(destination);

            Assert.IsTrue(helper.TryCopyFrom(new[] { 1, 2, 3 }));
            Assert.IsTrue(helper.TryCopyFrom(new[] { 4, 5, 6 }, 2));
            Assert.IsTrue(helper.TryCopyFrom(2, new[] { 7, 8 }));
            Assert.IsTrue(helper.TryCopyFrom(4, new[] { 9, 10, 11 }, 2));
            CollectionAssert.AreEqual(new[] { 4, 5, 7, 8, 9, 10 }, destination);

            var snapshot = destination.AsSpan().ToArray();

            Assert.IsFalse(helper.TryCopyFrom(5, new[] { 20, 21 }));
            Assert.IsFalse(helper.TryCopyFrom(new[] { 20 }, 2));
            CollectionAssert.AreEqual(snapshot, destination);
            Assert.Throws<ArgumentOutOfRangeException>(CopyFromInvalidWindow);
        }

        private static void SliceFromInvalidStart()
        {
            var helper = new CopyFromSpan<int>(new int[4]);
            helper.Slice(-1);
        }

        private static void SliceFromInvalidRange()
        {
            var helper = new CopyFromSpan<int>(new int[4]);
            helper.Slice(3, 2);
        }

        private static void CopyFromInvalidWindow()
        {
            var helper = new CopyFromSpan<int>(new int[4]);
            helper.CopyFrom(3, new[] { 1, 2 });
        }
    }
}
