using System;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Debugging
{
    public sealed class ThrowHelperTests
    {
        [Test]
        public void GetCollectionTypeName_UnknownOrUndefined_ReturnsCollection()
        {
            Assert.That(
                ThrowHelper.GetCollectionTypeName(ThrowHelper.CollectionType.Unknown),
                Is.EqualTo("collection")
            );
            Assert.That(
                ThrowHelper.GetCollectionTypeName((ThrowHelper.CollectionType)int.MaxValue),
                Is.EqualTo("collection")
            );
        }

        [Test]
        public void GetCollectionTypeName_ArrayUnsafe_ReturnsCurrentTypeName()
        {
            var name = ThrowHelper.GetCollectionTypeName(ThrowHelper.CollectionType.ArrayUnsafe);

            Assert.That(name, Is.EqualTo("ArrayUnsafe<T>"));
        }

        [Test]
        public void ThrowIfUnsafeCollectionIsDisposed_Invalid_ThrowsObjectDisposedException()
        {
            var exception = Assert.Throws<ObjectDisposedException>(() =>
                ThrowHelper.ThrowIfUnsafeCollectionIsDisposed(false, ThrowHelper.CollectionType.ArrayUnsafe)
            );

            Assert.That(exception!.Message, Is.EqualTo("The ArrayUnsafe<T> is already disposed."));
        }

        [Test]
        public void ThrowIfUnsafeCollectionAllocatorIsInvalid_Invalid_ThrowsInvalidOperationException()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ThrowHelper.ThrowIfUnsafeCollectionAllocatorIsInvalid(false, ThrowHelper.CollectionType.ReferenceUnsafe)
            );

            Assert.That(
                exception!.Message,
                Is.EqualTo(
                    "The ReferenceUnsafe<T> can not be Disposed because it was not allocated with a valid allocator."
                )
            );
        }

        [Test]
        public void ThrowIfUnsafeCollectionTypeIsManaged_Invalid_ThrowsInvalidOperationException()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ThrowHelper.ThrowIfUnsafeCollectionTypeIsManaged<int>(
                    false,
                    ThrowHelper.CollectionType.ReferenceUnsafe
                )
            );

            Assert.That(
                exception!.Message,
                Is.EqualTo(
                    "System.Int32 used in ReferenceUnsafe<T> must be unmanaged (contain no managed types)."
                )
            );
        }

        [Test]
        public void ThrowIfEmpty_InvalidQueue_ThrowsInvalidOperationException()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ThrowHelper.ThrowIfEmpty(false, ThrowHelper.CollectionType.QueueUnsafe)
            );

            Assert.That(exception!.Message, Is.EqualTo("the queue is empty"));
        }

        [Test]
        public void ThrowIfSourceCollectionIsNotCreated_Invalid_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                ThrowHelper.ThrowIfSourceCollectionIsNotCreated(false, ThrowHelper.CollectionType.ArraySetUnsafe)
            );

            Assert.That(exception!.ParamName, Is.EqualTo("source"));
            Assert.That(exception.Message, Does.StartWith("The source set is not created."));
        }

        [Test]
        public void ThrowIfNativeSourceCollectionIsNotCreated_Invalid_ThrowsInvalidOperationException()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ThrowHelper.ThrowIfNativeSourceCollectionIsNotCreated(
                    false,
                    ThrowHelper.CollectionType.ArrayMapNative
                )
            );

            Assert.That(exception!.Message, Is.EqualTo("The source map is not created."));
        }

        [Test]
        public void ThrowIfArgumentIndexIsNegative_Invalid_ThrowsArgumentOutOfRangeException()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThrowHelper.ThrowIfArgumentIndexIsNegative(false)
            );

            Assert.That(exception!.ParamName, Is.EqualTo("index"));
        }
    }
}
