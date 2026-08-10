#if ENABLE_UNITY_COLLECTIONS_CHECKS

using System;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    public class EncosyCollectionSafetyAPITests
    {
        [Test]
        public void PersistentHandle_DisposeReleasesHandleAndRejectsAccess()
        {
            var handle = EncosyCollectionSafetyAPI.CreateSafetyHandle(Allocator.Persistent);
            var valid = true;

            try
            {
                AtomicSafetyHandle.CheckReadAndThrow(handle);
                EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref handle);
                valid = false;

                Assert.Throws<ObjectDisposedException>(
                    () => AtomicSafetyHandle.CheckReadAndThrow(handle)
                );
            }
            finally
            {
                if (valid)
                {
                    EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref handle);
                    valid = false;
                }
            }

            Assert.IsFalse(valid);
        }

        [Test]
        public void TempHandle_DisposeInvalidatesLocalHandleWithoutReleasingSharedHandle()
        {
            var handle = EncosyCollectionSafetyAPI.CreateSafetyHandle(Allocator.Temp);
            var valid = true;

            try
            {
                Assert.IsTrue(AtomicSafetyHandle.IsTempMemoryHandle(handle));
                EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref handle);
                valid = false;

                Assert.Throws<ObjectDisposedException>(
                    () => AtomicSafetyHandle.CheckReadAndThrow(handle)
                );

                var sharedHandle = AtomicSafetyHandle.GetTempMemoryHandle();
                AtomicSafetyHandle.CheckReadAndThrow(sharedHandle);
            }
            finally
            {
                if (valid)
                {
                    EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref handle);
                    valid = false;
                }
            }

            Assert.IsFalse(valid);
        }

        [Test]
        public void ReleaseAfterSchedule_InvalidatesPersistentAndTempLocalHandles()
        {
            var persistent = EncosyCollectionSafetyAPI.CreateSafetyHandle(Allocator.Persistent);
            var temp = EncosyCollectionSafetyAPI.CreateSafetyHandle(Allocator.Temp);
            var persistentValid = true;
            var tempValid = true;

            try
            {
                EncosyCollectionSafetyAPI.ReleaseSafetyHandleAfterSchedule(ref persistent);
                persistentValid = false;

                Assert.Throws<ObjectDisposedException>(
                    () => AtomicSafetyHandle.CheckReadAndThrow(persistent)
                );

                Assert.IsTrue(AtomicSafetyHandle.IsTempMemoryHandle(temp));
                EncosyCollectionSafetyAPI.ReleaseSafetyHandleAfterSchedule(ref temp);
                tempValid = false;

                Assert.Throws<ObjectDisposedException>(
                    () => AtomicSafetyHandle.CheckReadAndThrow(temp)
                );

                var sharedHandle = AtomicSafetyHandle.GetTempMemoryHandle();
                AtomicSafetyHandle.CheckReadAndThrow(sharedHandle);
            }
            finally
            {
                if (persistentValid)
                {
                    EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref persistent);
                    persistentValid = false;
                }

                if (tempValid)
                {
                    EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref temp);
                    tempValid = false;
                }
            }

            Assert.IsFalse(persistentValid);
            Assert.IsFalse(tempValid);
        }

        [Test]
        public void SetStaticSafetyId_CreatesZeroIdOnceAndReusesCachedId()
        {
            var first = EncosyCollectionSafetyAPI.CreateSafetyHandle(Allocator.Persistent);
            var second = EncosyCollectionSafetyAPI.CreateSafetyHandle(Allocator.Persistent);
            var firstValid = true;
            var secondValid = true;
            var sharedStaticId = 0;

            try
            {
                EncosyCollectionSafetyAPI.SetStaticSafetyId<int>(
                      ref first
                    , ref sharedStaticId
                );

                Assert.AreNotEqual(0, sharedStaticId);

                var cachedId = sharedStaticId;
                EncosyCollectionSafetyAPI.SetStaticSafetyId<int>(
                      ref second
                    , ref sharedStaticId
                );

                Assert.AreEqual(cachedId, sharedStaticId);
                AtomicSafetyHandle.CheckReadAndThrow(first);
                AtomicSafetyHandle.CheckReadAndThrow(second);
            }
            finally
            {
                if (firstValid)
                {
                    EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref first);
                    firstValid = false;
                }

                if (secondValid)
                {
                    EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref second);
                    secondValid = false;
                }
            }

            Assert.IsFalse(firstValid);
            Assert.IsFalse(secondValid);
        }
    }
}

#endif
