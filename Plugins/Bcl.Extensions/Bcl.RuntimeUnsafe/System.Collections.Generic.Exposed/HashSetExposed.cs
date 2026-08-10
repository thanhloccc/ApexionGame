#pragma warning disable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Generic.Exposed;

internal readonly struct HashSetExposed<T>([NotNull] HashSet<T> set)
{
    public readonly HashSet<T> Set = set;

    public ReadOnlySpan<int> Buckets
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Set._buckets);
    }

    public ReadOnlySpan<Slot> Slots
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var slots = Unsafe.As<HashSet<T>.Slot[], Slot[]>(ref Set._slots);
            return new ReadOnlySpan<Slot>(slots, 0, Set._count);
        }
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Set._count;
    }

    public int FreeList
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Set._freeList;
    }

    public int LastIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Set._lastIndex;
    }

    public int Version
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Set._version;
    }

    public IEqualityComparer<T> Comparer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Set._comparer;
    }

    public struct Slot
    {
        public int hashCode;
        public int next;
        public T value;
    }
}
