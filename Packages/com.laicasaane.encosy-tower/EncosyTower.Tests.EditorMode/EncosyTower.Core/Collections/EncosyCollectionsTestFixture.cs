using System;
using NUnit.Framework;
using Unity.Jobs.LowLevel.Unsafe;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/CollectionsTestFixture.cs.
    // The rewindable-allocator base (CollectionsTestCommonBase) is intentionally skipped:
    // Encosy collections allocate through AllocatorStrategy, not a rewindable allocator.

    // If ENABLE_UNITY_COLLECTIONS_CHECKS is not defined the test is ignored.
    // Logically ANDs with any other Test-requires attribute.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    internal class TestRequiresCollectionChecks : System.Attribute
    {
        public TestRequiresCollectionChecks(string msg = null) { }
    }
#else
    internal class TestRequiresCollectionChecks : IgnoreAttribute
    {
        public TestRequiresCollectionChecks(string msg = null)
            : base(
                "Test requires ENABLE_UNITY_COLLECTIONS_CHECKS which is not defined"
                + (msg == null ? "." : $": {msg}")
            )
        { }
    }
#endif

    /// <summary>
    /// Key whose every instance reports the same hash code, so every add after the first
    /// lands in one bucket chain. Used to regression-test collision-churn bookkeeping:
    /// the cumulative collision counter and the bucket-recompute path of the array maps.
    /// </summary>
    internal readonly struct CollidingKey : IEquatable<CollidingKey>
    {
        public readonly int Value;

        public CollidingKey(int value)
        {
            Value = value;
        }

        public bool Equals(CollidingKey other)
            => Value == other.Value;

        public override bool Equals(object obj)
            => obj is CollidingKey other && Equals(other);

        public override int GetHashCode()
            => 7;
    }

    /// <summary>
    /// Base fixture that forces the Jobs Debugger enabled for the duration of a test
    /// and restores the previous value on teardown, mirroring Unity's collections tests.
    /// </summary>
    internal abstract class EncosyCollectionsTestFixture
    {
        private bool _jobsDebuggerWasEnabled;

        [SetUp]
        public virtual void Setup()
        {
            _jobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;
        }

        [TearDown]
        public virtual void TearDown()
        {
            JobsUtility.JobDebuggerEnabled = _jobsDebuggerWasEnabled;
        }
    }
}
