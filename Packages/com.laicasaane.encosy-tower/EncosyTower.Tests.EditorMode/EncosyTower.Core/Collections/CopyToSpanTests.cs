using System;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public class CopyToSpanTests
    {
        [Test]
        public void ConstructionConversionsLengthAndSlices_ExposeExpectedWindows()
        {
            ReadOnlySpan<int> values = new[] { 1, 2, 3, 4 };
            var direct = new CopyToSpan<int>(values);
            CopyToSpan<int> converted = values;
            ReadOnlySpan<int> span = converted;

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
            var helper = new CopyToSpan<int>(new[] { 1, 2, 3, 4 });

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
        public void CopyTo_AllOverloadsCopyRequestedWindows()
        {
            var helper = new CopyToSpan<int>(new[] { 1, 2, 3, 4, 5, 6 });
            var all = new int[6];
            var first = new int[3];
            var sourceWindow = new int[2];
            var explicitWindow = new int[3];

            helper.CopyTo(all);
            helper.CopyTo(first, 3);
            helper.CopyTo(2, sourceWindow);
            helper.CopyTo(1, explicitWindow, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, all);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, first);
            CollectionAssert.AreEqual(new[] { 3, 4 }, sourceWindow);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitWindow);
        }

        [Test]
        public void TryCopyTo_AllOverloadsReturnExpectedStatusAndDoNotPartiallyWrite()
        {
            var helper = new CopyToSpan<int>(new[] { 1, 2, 3, 4, 5, 6 });
            var all = new int[6];
            var first = new int[3];
            var sourceWindow = new int[2];
            var explicitWindow = new int[3];

            Assert.IsTrue(helper.TryCopyTo(all));
            Assert.IsTrue(helper.TryCopyTo(first, 3));
            Assert.IsTrue(helper.TryCopyTo(2, sourceWindow));
            Assert.IsTrue(helper.TryCopyTo(1, explicitWindow, 2));
            CollectionAssert.AreEqual(new[] { 3, 4 }, sourceWindow);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitWindow);

            var destination = new[] { 9, 9 };

            Assert.IsFalse(helper.TryCopyTo(destination, 3));
            Assert.IsFalse(helper.TryCopyTo(5, destination));
            CollectionAssert.AreEqual(new[] { 9, 9 }, destination);
            Assert.Throws<ArgumentOutOfRangeException>(CopyToInvalidWindow);
        }

        private static void SliceFromInvalidStart()
        {
            var helper = new CopyToSpan<int>(new[] { 1, 2, 3, 4 });
            helper.Slice(-1);
        }

        private static void SliceFromInvalidRange()
        {
            var helper = new CopyToSpan<int>(new[] { 1, 2, 3, 4 });
            helper.Slice(3, 2);
        }

        private static void CopyToInvalidWindow()
        {
            var helper = new CopyToSpan<int>(new[] { 1, 2, 3, 4 });
            helper.CopyTo(3, new int[2]);
        }
    }
}
