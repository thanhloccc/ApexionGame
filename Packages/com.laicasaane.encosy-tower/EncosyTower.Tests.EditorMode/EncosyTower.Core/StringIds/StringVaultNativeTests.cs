using EncosyTower.StringIds;
using EncosyTower.Tests.Core.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.StringIds
{
    // Adapted from Unity.Collections.Tests/NativeHashMapTests.cs (id/lookup patterns)
    // against the StringVaultNative forwarder.
    public partial class StringVaultNativeTests
    {
        [Test]
        public void GetOrMakeId_SameString_ReturnsSameId()
        {
            using var vault = new StringVaultNative(4, Allocator.Temp);

            var id1 = vault.GetOrMakeId((UnmanagedString)"hello");
            var id2 = vault.GetOrMakeId((UnmanagedString)"hello");

            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void GetOrMakeId_DifferentStrings_ReturnsDifferentIds()
        {
            using var vault = new StringVaultNative(4, Allocator.Temp);

            var idA = vault.GetOrMakeId((UnmanagedString)"apple");
            var idB = vault.GetOrMakeId((UnmanagedString)"banana");

            Assert.AreNotEqual(idA, idB);
        }

        [Test]
        public void TryGetId_Existing_MatchesGetOrMakeId()
        {
            using var vault = new StringVaultNative(4, Allocator.Temp);
            var id = vault.GetOrMakeId((UnmanagedString)"world");

            Assert.IsTrue(vault.TryGetId((UnmanagedString)"world", out var found));
            Assert.AreEqual(id, found);
        }

        [Test]
        public void TryGetId_Missing_ReturnsFalse()
        {
            using var vault = new StringVaultNative(4, Allocator.Temp);
            vault.GetOrMakeId((UnmanagedString)"present");

            Assert.IsFalse(vault.TryGetId((UnmanagedString)"absent", out _));
        }

        [Test]
        public void TryGetUnmanagedString_RoundTrips()
        {
            using var vault = new StringVaultNative(4, Allocator.Temp);
            var id = vault.GetOrMakeId((UnmanagedString)"round-trip");

            Assert.IsTrue(vault.TryGetUnmanagedString(id, out var back));
            Assert.AreEqual("round-trip", back.ToString());
        }

        [Test]
        public void ContainsId_ReflectsRegistration()
        {
            using var vault = new StringVaultNative(4, Allocator.Temp);
            var id = vault.GetOrMakeId((UnmanagedString)"registered");

            Assert.IsTrue(vault.ContainsId(id));
        }

        [Test]
        public void Clear_ResetsRegistry()
        {
            using var vault = new StringVaultNative(4, Allocator.Temp);
            vault.GetOrMakeId((UnmanagedString)"temp");

            vault.Clear();

            Assert.IsFalse(vault.TryGetId((UnmanagedString)"temp", out _));

            vault.GetOrMakeId((UnmanagedString)"fresh");
            Assert.IsTrue(vault.TryGetId((UnmanagedString)"fresh", out _));
        }

        [Test]
        public void RoundTrip_ManyStrings_GrowsAndResolves()
        {
            const int N = 40;
            using var vault = new StringVaultNative(4, Allocator.Temp);

            var ids = new StringId[N];

            for (var i = 0; i < N; i++)
            {
                ids[i] = vault.GetOrMakeId((UnmanagedString)$"str{i}");
            }

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(vault.TryGetUnmanagedString(ids[i], out var back), $"id {i}");
                Assert.AreEqual($"str{i}", back.ToString());

                Assert.IsTrue(vault.TryGetId((UnmanagedString)$"str{i}", out var id), $"str{i}");
                Assert.AreEqual(ids[i], id);
            }
        }

        [Test]
        public void Seeding_Constructor_RegistersAll()
        {
            var seed = new[]
            {
                (UnmanagedString)"alpha",
                (UnmanagedString)"beta",
                (UnmanagedString)"gamma",
            };

            using var vault = new StringVaultNative(seed, Allocator.Temp);

            foreach (var str in seed)
            {
                Assert.IsTrue(vault.TryGetId(str, out _), str.ToString());
            }
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var vault = new StringVaultNative(4, Allocator.Temp);
            Assert.IsTrue(vault.IsCreated);

            vault.Dispose();

            Assert.IsFalse(vault.IsCreated);
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var vault = new StringVaultNative(4, Allocator.Persistent);
            vault.GetOrMakeId((UnmanagedString)"value");

            vault.Dispose(default(JobHandle)).Complete();

            Assert.IsFalse(vault.IsCreated);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void UseAfterDispose_Throws()
        {
            var vault = new StringVaultNative(4, Allocator.Temp);
            vault.GetOrMakeId((UnmanagedString)"x");
            vault.Dispose();

            Assert.Catch(() => _ = vault.Count);
        }
    }
}
