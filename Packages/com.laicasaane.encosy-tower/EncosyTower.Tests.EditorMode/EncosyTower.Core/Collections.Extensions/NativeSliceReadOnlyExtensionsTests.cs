using NUnit.Framework;
using Unity.Collections;

using SliceAPI = EncosyTower.Collections.Extensions.NativeSliceReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections.Extensions
{
    public partial class NativeSliceReadOnlyExtensionsTests
    {
        [Test]
        public void AsReadOnly_ExposesNativeSliceWindow()
        {
            using var array = new NativeArray<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var slice = new NativeSlice<int>(array, 1, 2);
            var readOnly = SliceAPI.AsReadOnly(slice);

            Assert.AreEqual(2, readOnly.Length);
            Assert.AreEqual(2, readOnly[0]);
            Assert.AreEqual(3, readOnly[1]);
        }

        [Test]
        public void Slice_ArrayAndNestedWindowsExposeExpectedValues()
        {
            using var array = new NativeArray<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var readOnlyArray = array.AsReadOnly();
            var full = SliceAPI.Slice(readOnlyArray);
            var fromStart = SliceAPI.Slice(readOnlyArray, 1);
            var window = SliceAPI.Slice(readOnlyArray, 1, 2);
            var nestedFull = SliceAPI.Slice(window);
            var nestedFromStart = SliceAPI.Slice(window, 1);
            var nestedWindow = SliceAPI.Slice(full, 1, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, fromStart.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3 }, window.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3 }, nestedFull.ToArray());
            CollectionAssert.AreEqual(new[] { 3 }, nestedFromStart.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3 }, nestedWindow.ToArray());
        }
    }
}
