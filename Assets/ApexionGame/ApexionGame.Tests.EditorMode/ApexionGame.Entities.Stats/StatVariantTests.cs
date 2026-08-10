using NUnit.Framework;
using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Tests
{
    public sealed class StatVariantTests
    {
        [Test]
        public void Ctor_SetsTypeAndValue()
        {
            Assert.AreEqual(StatVariantType.Float, new StatVariant(1.5f).Type);
            Assert.AreEqual(StatVariantType.Int, new StatVariant(7).Type);
            Assert.AreEqual(StatVariantType.Bool, new StatVariant(true).Type);
            Assert.AreEqual(1.5f, new StatVariant(1.5f).Float);
            Assert.AreEqual(7, new StatVariant(7).Int);
            Assert.IsTrue(new StatVariant(true).Bool);
        }

        [Test]
        public void Add_Float()
        {
            var result = new StatVariant(2.5f) + new StatVariant(4f);

            Assert.AreEqual(StatVariantType.Float, result.Type);
            Assert.AreEqual(6.5f, result.Float);
        }

        [Test]
        public void Add_KeepsNarrowTypeInsteadOfPromotingToInt()
        {
            var result = new StatVariant((byte)200) + new StatVariant((byte)55);

            Assert.AreEqual(StatVariantType.Byte, result.Type);
            Assert.AreEqual((byte)255, result.Byte);
        }

        [Test]
        public void Subtract_Multiply_Divide_Int()
        {
            Assert.AreEqual(4, (new StatVariant(10) - new StatVariant(6)).Int);
            Assert.AreEqual(60, (new StatVariant(10) * new StatVariant(6)).Int);
            Assert.AreEqual(1, (new StatVariant(10) / new StatVariant(6)).Int);
        }

        [Test]
        public void Arithmetic_Float4_IsComponentWise()
        {
            var a = new StatVariant(new float4(1f, 2f, 3f, 4f));
            var b = new StatVariant(new float4(10f, 20f, 30f, 40f));
            var result = a + b;

            Assert.AreEqual(StatVariantType.Float4, result.Type);
            Assert.AreEqual(new float4(11f, 22f, 33f, 44f), result.Float4);
        }

        [Test]
        public void Arithmetic_Half_RoundTripsThroughFloat()
        {
            var result = new StatVariant(math.half(1.5f)) + new StatVariant(math.half(2.25f));

            Assert.AreEqual(StatVariantType.Half, result.Type);
            Assert.AreEqual(3.75f, (float)result.Half);
        }

        [Test]
        public void UnaryMinus_Negates()
        {
            Assert.AreEqual(-5, (-new StatVariant(5)).Int);
            Assert.AreEqual(-2.5, (-new StatVariant(2.5)).Double);
        }

        [Test]
        public void UnaryMinus_RejectsUnsignedTypes()
        {
            Assert.Throws<StatVariantOperatorException>(() => _ = -new StatVariant(5u));
            Assert.Throws<StatVariantOperatorException>(() => _ = -new StatVariant((byte)5));
            Assert.Throws<StatVariantOperatorException>(() => _ = -new StatVariant((ushort)5));
            Assert.Throws<StatVariantOperatorException>(() => _ = -new StatVariant(5UL));
        }

        [Test]
        public void UnaryPlus_AcceptsUnsignedTypes()
        {
            Assert.AreEqual(5u, (+new StatVariant(5u)).UInt);
            Assert.AreEqual((byte)5, (+new StatVariant((byte)5)).Byte);
        }

        [Test]
        public void Not_OnlyAcceptsBool()
        {
            Assert.IsFalse((!new StatVariant(true)).Bool);
            Assert.Throws<StatVariantOperatorException>(() => _ = !new StatVariant(1));
        }

        [Test]
        public void BitwiseOps_AcceptIntegersAndBool()
        {
            Assert.AreEqual(0b0100, (new StatVariant(0b0110) & new StatVariant(0b1100)).Int);
            Assert.AreEqual(0b1110, (new StatVariant(0b0110) | new StatVariant(0b1100)).Int);
            Assert.AreEqual(0b1010, (new StatVariant(0b0110) ^ new StatVariant(0b1100)).Int);
            Assert.IsTrue((new StatVariant(true) & new StatVariant(true)).Bool);
            Assert.IsFalse((new StatVariant(true) ^ new StatVariant(true)).Bool);
        }

        [Test]
        public void BitwiseOps_RejectFloatingPoint()
        {
            Assert.Throws<StatVariantOperatorException>(
                () => _ = new StatVariant(1f) & new StatVariant(1f)
            );

            Assert.Throws<StatVariantOperatorException>(() => _ = ~new StatVariant(1f));
        }

        [Test]
        public void Shifts_AcceptIntegers()
        {
            Assert.AreEqual(8, (new StatVariant(1) << 3).Int);
            Assert.AreEqual(1, (new StatVariant(8) >> 3).Int);
            Assert.Throws<StatVariantOperatorException>(() => _ = new StatVariant(1f) << 3);
        }

        [Test]
        public void Complement_KeepsNarrowType()
        {
            var result = ~new StatVariant((sbyte)0);

            Assert.AreEqual(StatVariantType.SByte, result.Type);
            Assert.AreEqual((sbyte)-1, result.SByte);
        }

        [Test]
        public void MinMax_Scalar()
        {
            Assert.AreEqual(2f, StatVariant.Min(new StatVariant(2f), new StatVariant(5f)).Float);
            Assert.AreEqual(5f, StatVariant.Max(new StatVariant(2f), new StatVariant(5f)).Float);
        }

        [Test]
        public void MinMax_UnsignedDoesNotWrapThroughSignedCompare()
        {
            var big = new StatVariant((byte)200);
            var small = new StatVariant((byte)5);

            Assert.AreEqual((byte)5, StatVariant.Min(big, small).Byte);
            Assert.AreEqual((byte)200, StatVariant.Max(big, small).Byte);
        }

        [Test]
        public void Clamp_Scalar()
        {
            var lo = new StatVariant(0f);
            var hi = new StatVariant(10f);

            Assert.AreEqual(0f, StatVariant.Clamp(new StatVariant(-5f), lo, hi).Float);
            Assert.AreEqual(10f, StatVariant.Clamp(new StatVariant(50f), lo, hi).Float);
            Assert.AreEqual(4f, StatVariant.Clamp(new StatVariant(4f), lo, hi).Float);
        }

        [Test]
        public void Clamp_Float3_IsComponentWise()
        {
            var value = new StatVariant(new float3(-1f, 5f, 99f));
            var lo = new StatVariant(new float3(0f));
            var hi = new StatVariant(new float3(10f));

            Assert.AreEqual(new float3(0f, 5f, 10f), StatVariant.Clamp(value, lo, hi).Float3);
        }

        [Test]
        public void Clamp_RejectsBool()
        {
            var value = new StatVariant(true);

            Assert.Throws<StatVariantOperatorException>(() => _ = StatVariant.Clamp(value, value, value));
        }

        [Test]
        public void MismatchedOperandTypes_Throw()
        {
            Assert.Throws<StatVariantTypeException>(
                () => _ = new StatVariant(1f) + new StatVariant(1)
            );

            Assert.Throws<StatVariantTypeException>(
                () => _ = StatVariant.Min(new StatVariant(1f), new StatVariant(1))
            );
        }

        [Test]
        public void MismatchedClampTypes_Throw()
        {
            Assert.Throws<StatVariantTypeException>(
                () => _ = StatVariant.Clamp(new StatVariant(1f), new StatVariant(0f), new StatVariant(0))
            );
        }

        [Test]
        public void NoneType_RejectsArithmetic()
        {
            var none = new StatVariant(new None());

            Assert.AreEqual(StatVariantType.None, none.Type);
            Assert.Throws<StatVariantOperatorException>(() => _ = none + none);
        }
    }
}
