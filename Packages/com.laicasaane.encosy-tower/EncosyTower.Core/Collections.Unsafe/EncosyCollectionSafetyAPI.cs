using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Unsafe
{
    /// <summary>
    /// Core-only replacement for the safety-handle helpers found in
    /// <c>Unity.Collections.CollectionHelper</c>. It is built purely on
    /// <see cref="AtomicSafetyHandle"/> (which lives in UnityEngine.CoreModule),
    /// so it does not require the <c>com.unity.collections</c> package.
    /// <br/>
    /// Every member only exists when <c>ENABLE_UNITY_COLLECTIONS_CHECKS</c> is on.
    /// Callers must guard their calls with the same symbol, mirroring how the
    /// Unity native containers do it.
    /// </summary>
    public static class EncosyCollectionSafetyAPI
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        /// <summary>
        /// Creates a safety handle that stays valid until
        /// <see cref="DisposeSafetyHandle(ref AtomicSafetyHandle)"/> is called.
        /// </summary>
        /// <remarks>
        /// A <see cref="Allocator.Temp"/> allocation reuses the shared temp handle
        /// (<see cref="AtomicSafetyHandle.GetTempMemoryHandle"/>) instead of creating
        /// a brand new handle, otherwise the new handle would leak.
        /// </remarks>
        public static AtomicSafetyHandle CreateSafetyHandle(Allocator allocator)
        {
            return allocator == Allocator.Temp
                ? AtomicSafetyHandle.GetTempMemoryHandle()
                : AtomicSafetyHandle.Create();
        }

        /// <summary>
        /// Releases a handle created by
        /// <see cref="CreateSafetyHandle(Allocator)"/>.
        /// </summary>
        /// <remarks>
        /// A temp handle is shared, so it cannot be released directly. Instead a
        /// throw-away real handle is created and released in its place, which lets
        /// the job debugger keep reporting use-after-dispose correctly.
        /// </remarks>
        public static void DisposeSafetyHandle(ref AtomicSafetyHandle handle)
        {
            AtomicSafetyHandle.CheckDeallocateAndThrow(handle);

            // If the safety handle is for a temp allocation, swap in a fresh real
            // handle for this instance so it can be marked invalid. Reassigning to a
            // default handle is not enough because a valid node pointer is required
            // to produce the correct errors.
            if (AtomicSafetyHandle.IsTempMemoryHandle(handle))
            {
                handle = AtomicSafetyHandle.Create();
            }

            AtomicSafetyHandle.Release(handle);
        }

        /// <summary>
        /// Releases a handle right after a dispose job has been scheduled
        /// (the <c>Dispose(JobHandle)</c> pattern). Unlike
        /// <see cref="DisposeSafetyHandle(ref AtomicSafetyHandle)"/> it performs no
        /// deallocation check — the caller has already validated the handle.
        /// </summary>
        /// <remarks>
        /// The shared temp handle must never be released directly; swap in a fresh
        /// real handle for this instance so it can be marked invalid.
        /// </remarks>
        public static void ReleaseSafetyHandleAfterSchedule(ref AtomicSafetyHandle handle)
        {
            if (AtomicSafetyHandle.IsTempMemoryHandle(handle))
            {
                handle = AtomicSafetyHandle.Create();
            }

            AtomicSafetyHandle.Release(handle);
        }

        /// <summary>
        /// Assigns a static safety id to <paramref name="handle"/>. The id carries the
        /// owner type name that the job debugger uses when reporting errors. The id is
        /// created lazily and cached in <paramref name="sharedStaticId"/>.
        /// </summary>
        public static void SetStaticSafetyId<T>(ref AtomicSafetyHandle handle, ref int sharedStaticId)
        {
            if (sharedStaticId == 0)
            {
                CreateStaticSafetyId<T>(ref sharedStaticId);
            }

            AtomicSafetyHandle.SetStaticSafetyId(ref handle, sharedStaticId);
        }

        // Creating the id reflects over typeof(T) and encodes a managed string, so it
        // is discarded under Burst (the id simply stays 0, which is a valid value).
#if UNITY_BURST
        [Unity.Burst.BurstDiscard]
#endif
        private static void CreateStaticSafetyId<T>(ref int id)
        {
            id = AtomicSafetyHandle.NewStaticSafetyId<T>();
        }
#endif
    }
}
