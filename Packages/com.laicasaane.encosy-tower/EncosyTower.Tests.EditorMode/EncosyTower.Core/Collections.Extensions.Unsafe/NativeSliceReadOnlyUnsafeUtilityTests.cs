using System;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using SliceAPI = EncosyTower.Collections.Unsafe.NativeSliceReadOnlyUnsafeUtility;

namespace EncosyTower.Tests.Core.Collections.Extensions.Unsafe
{
    public partial class NativeSliceReadOnlyUnsafeUtilityTests
    {
        [Test]
        public void UnsafePointers_MatchSourceArrayAndAliasStorage()
        {
            using var array = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var slice = new NativeSliceReadOnly<int>(array);
            var writePointer = IntPtr.Zero;
            var readPointer = IntPtr.Zero;
            var arrayPointer = IntPtr.Zero;

            // SAFETY: The array owns the live allocation for the entire pointer comparison and write.
            unsafe
            {
                writePointer = (IntPtr)SliceAPI.GetUnsafePtr(slice);
                readPointer = (IntPtr)SliceAPI.GetUnsafeReadOnlyPtr(slice);
                arrayPointer = (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(array);
                *(int*)writePointer = 99;
            }

            Assert.AreNotEqual(IntPtr.Zero, writePointer);
            Assert.AreEqual(arrayPointer, writePointer);
            Assert.AreEqual(arrayPointer, readPointer);
            Assert.AreEqual(99, array[0]);
        }
    }
}
