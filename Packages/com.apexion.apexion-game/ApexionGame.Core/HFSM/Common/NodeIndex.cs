using System;
using System.Runtime.CompilerServices;
using EncosyTower.Common;
using EncosyTower.TypeWraps;
using Unity.Collections;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// A node's position in <see cref="MachineDefinition{TContext, TState}"/>'s flat node array.
    /// </summary>
    /// <remarks>
    /// A typed index so a node index can never be passed where a transition index is expected.
    /// <para>
    /// <b><see langword="default"/> is node 0, which is the root — not an invalid value.</b> The
    /// indices are dense and start at zero because they index straight into the baked arrays, so
    /// there is no spare bit pattern to spend on a sentinel. Absence is always
    /// <see cref="Invalid"/>, never <see langword="default"/>.
    /// </para>
    /// </remarks>
    [Serializable, WrapType(typeof(int), nameof(value))]
    public partial struct NodeIndex : IIsValid
    {
        /// <summary>
        /// The absence of a node. Distinct from <see langword="default"/>, which is the root node.
        /// </summary>
        public static readonly NodeIndex Invalid = Of(-1);

        public int value;

        public readonly bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => value >= 0;
        }

        /// <summary>
        /// Wraps a raw index.
        /// </summary>
        /// <remarks>
        /// A factory rather than a constructor: <see cref="WrapTypeAttribute"/> generates the
        /// conversion surface, and a hand-written constructor risks colliding with it.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NodeIndex Of(int value)
            => new() { value = value };

        /// <summary>
        /// Verifies that this index addresses a real slot of an array of <paramref name="length"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsWithin(int length)
            => (uint)value < (uint)length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FixedString32Bytes ToFixedString()
        {
            FixedString32Bytes result = default;
            result.Append(value);
            return result;
        }
    }
}
