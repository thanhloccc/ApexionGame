using System;
using System.Runtime.CompilerServices;
using EncosyTower.Common;
using Unity.Collections;

namespace ApexionGame.Entities.Stats
{
    public readonly struct StatOwnerHandle : IEquatable<StatOwnerHandle>, IIsValid
    {
        public static readonly StatOwnerHandle Null = default;

        public readonly int Index;
        public readonly int Version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatOwnerHandle(int index, int version)
        {
            Index = index;
            Version = version;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Version > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(StatOwnerHandle left, StatOwnerHandle right)
            => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(StatOwnerHandle left, StatOwnerHandle right)
            => left.Equals(right) == false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int index, out int version)
        {
            index = Index;
            version = Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StatOwnerHandle other)
            => Index == other.Index && Version == other.Version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
            => obj is StatOwnerHandle other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => HashValue.Combine(Index, Version);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => $"Owner({Index}:{Version})";

        public FixedString32Bytes ToFixedString()
        {
            var fs = new FixedString32Bytes();
            fs.Append('O');
            fs.Append('(');
            fs.Append(Index);
            fs.Append(':');
            fs.Append(Version);
            fs.Append(')');

            return fs;
        }
    }
}
