namespace EncosyTower.Debugging
{
    using System;
    using System.Diagnostics;
    using JetBrains.Annotations;

    using static ValidationDefines;

    using Debug = UnityEngine.Debug;

    [DebuggerStepThrough]
    public static partial class Checks
    {
        [AssertionMethod]
        [ContractAnnotation("condition:false=>halt")]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG), Conditional(RUNTIME_CHECKS)]
        public static void IsTrue(bool condition)
        {
            Debug.Assert(condition);
        }

        [AssertionMethod]
        [ContractAnnotation("condition:false=>halt")]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG), Conditional(RUNTIME_CHECKS)]
        public static void IsTrue(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        [AssertionMethod]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG), Conditional(RUNTIME_CHECKS)]
        public static void IndexInRange(int index, int length)
        {
            if ((uint)index >= (uint)length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in container of '{length}' Length.");
            }
        }

        [AssertionMethod]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG), Conditional(RUNTIME_CHECKS)]
        public static void OneIndexInRange(int index, int length)
        {
            if (index < 1 || (uint)index >= (uint)length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in container of '{length}' Length.");
            }
        }
    }
}

#if UNITY_BURST

namespace EncosyTower.Debugging
{
    using System.Runtime.CompilerServices;
    using Unity.Burst.CompilerServices;

    public static partial class Checks
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: AssumeRange(0L, 2147483647L)]
        public static int BurstAssumePositive(int value)
        {
            return value;
        }
    }
}

#endif
