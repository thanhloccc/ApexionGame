using System;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;

namespace EncosyTower.Collections.Extensions
{
    public static class ArrayMapUnsafeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TValue> GetValues<TKey, TValue>(this in ArrayMapUnsafe<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
                => self._values.AsSpan()[..self._freeValueCellIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue>(
              this ref ArrayMapUnsafe<TKey, TValue> self
            , TKey key
            , Func<TValue> builder
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (self.TryFindIndex(key, out var index))
            {
                self._version++;
                return ref self._values[index];
            }

            self.AddValue(key, out index);

            self._values[index] = builder();

            return ref self._values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue, TParam>(
              this ref ArrayMapUnsafe<TKey, TValue> self
            , TKey key
            , FuncRef<TParam, TValue> builder
            , ref TParam parameter
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (self.TryFindIndex(key, out var index))
            {
                self._version++;
                return ref self._values[index];
            }

            self.AddValue(key, out index);

            self._values[index] = builder(ref parameter);

            return ref self._values[index];
        }

        /// <summary>
        /// Gets the value for <paramref name="key"/>, or adds an entry when the key is not present.
        /// Intended for maps that are fast-cleared: when the new slot still holds data
        /// from before the clear, that data can be recycled instead of building a new value.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="builder">Creates a new value when the slot is not recycled.</param>
        /// <param name="recycler">Resets the leftover data before it is reused.</param>
        /// <param name="shouldBeRecycled">Decides whether the leftover data can be recycled.</param>
        /// <returns>A reference to the value for <paramref name="key"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue RecycleOrAdd<TKey, TValue>(
              this ref ArrayMapUnsafe<TKey, TValue> self
            , TKey key
            , Func<TValue> builder
            , ActionRef<TValue> recycler
            , PredicateRef<TValue> shouldBeRecycled
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (self.TryFindIndex(key, out var index))
            {
                self._version++;
                return ref self._values[index];
            }

            self.AddValue(key, out index);

            if (shouldBeRecycled(ref self._values[index]))
            {
                recycler(ref self._values[index]);
            }
            else
            {
                self._values[index] = builder();
            }

            return ref self._values[index];
        }

        /// <summary>
        /// Gets the value for <paramref name="key"/>, or adds an entry when the key is not present.
        /// Intended for maps that are fast-cleared: when the new slot still holds data
        /// from before the clear, that data can be recycled instead of building a new value.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="builder">Creates a new value when the slot is not recycled.</param>
        /// <param name="recycler">Resets the leftover data before it is reused.</param>
        /// <param name="shouldBeRecycled">Decides whether the leftover data can be recycled.</param>
        /// <param name="parameter">State passed by reference to <paramref name="builder"/>
        /// and <paramref name="recycler"/>.</param>
        /// <typeparam name="TParam">The type of <paramref name="parameter"/>.</typeparam>
        /// <returns>A reference to the value for <paramref name="key"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue RecycleOrAdd<TKey, TValue, TParam>(
              this ref ArrayMapUnsafe<TKey, TValue> self
            , TKey key
            , FuncRef<TParam, TValue> builder
            , ActionRef<TValue, TParam> recycler
            , PredicateRef<TValue> shouldBeRecycled
            , ref TParam parameter
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (self.TryFindIndex(key, out var index))
            {
                self._version++;
                return ref self._values[index];
            }

            self.AddValue(key, out index);

            if (shouldBeRecycled(ref self._values[index]))
            {
                recycler(ref self._values[index], ref parameter);
            }
            else
            {
                self._values[index] = builder(ref parameter);
            }

            return ref self._values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue, TBuilder>(
              this ref ArrayMapUnsafe<TKey, TValue> self
            , TKey key
            , ref TBuilder builder
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where TBuilder : IFunc<TValue>
        {
            if (self.TryFindIndex(key, out var index))
            {
                self._version++;
                return ref self._values[index];
            }

            self.AddValue(key, out index);

            self._values[index] = builder.Invoke();

            return ref self._values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAdd<TKey, TValue, TParam, TBuilder>(
              this ref ArrayMapUnsafe<TKey, TValue> self
            , TKey key
            , ref TBuilder builder
            , ref TParam parameter
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where TBuilder : IFuncRef<TParam, TValue>
        {
            if (self.TryFindIndex(key, out var index))
            {
                self._version++;
                return ref self._values[index];
            }

            self.AddValue(key, out index);

            self._values[index] = builder.Invoke(ref parameter);

            return ref self._values[index];
        }

        /// <summary>
        /// Gets the value for <paramref name="key"/>, or adds an entry when the key is not present.
        /// Intended for maps that are fast-cleared: when the new slot still holds data
        /// from before the clear, that data can be recycled instead of building a new value.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="builder">Creates a new value when the slot is not recycled.</param>
        /// <param name="recycler">Resets the leftover data before it is reused.</param>
        /// <param name="shouldBeRecycled">Decides whether the leftover data can be recycled.</param>
        /// <returns>A reference to the value for <paramref name="key"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue RecycleOrAdd<TKey, TValue, TBuilder, TRecyler, TShouldBeRecycled>(
              this ref ArrayMapUnsafe<TKey, TValue> self
            , TKey key
            , ref TBuilder builder
            , ref TRecyler recycler
            , ref TShouldBeRecycled shouldBeRecycled
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where TBuilder : IFunc<TValue>
            where TRecyler : IActionRef<TValue>
            where TShouldBeRecycled : IPredicateRef<TValue>
        {
            if (self.TryFindIndex(key, out var index))
            {
                self._version++;
                return ref self._values[index];
            }

            self.AddValue(key, out index);

            if (shouldBeRecycled.Invoke(ref self._values[index]))
            {
                recycler.Invoke(ref self._values[index]);
            }
            else
            {
                self._values[index] = builder.Invoke();
            }

            return ref self._values[index];
        }

        /// <summary>
        /// Gets the value for <paramref name="key"/>, or adds an entry when the key is not present.
        /// Intended for maps that are fast-cleared: when the new slot still holds data
        /// from before the clear, that data can be recycled instead of building a new value.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="builder">Creates a new value when the slot is not recycled.</param>
        /// <param name="recycler">Resets the leftover data before it is reused.</param>
        /// <param name="shouldBeRecycled">Decides whether the leftover data can be recycled.</param>
        /// <param name="parameter">State passed by reference to <paramref name="builder"/>
        /// and <paramref name="recycler"/>.</param>
        /// <typeparam name="TParam">The type of <paramref name="parameter"/>.</typeparam>
        /// <returns>A reference to the value for <paramref name="key"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue RecycleOrAdd<TKey, TValue, TParam, TBuilder, TRecyler, TShouldBeRecycled>(
              this ref ArrayMapUnsafe<TKey, TValue> self
            , TKey key
            , ref TBuilder builder
            , ref TRecyler recycler
            , ref TShouldBeRecycled shouldBeRecycled
            , ref TParam parameter
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where TBuilder : IFuncRef<TParam, TValue>
            where TRecyler : IActionRef<TValue, TParam>
            where TShouldBeRecycled : IPredicateRef<TValue>
        {
            if (self.TryFindIndex(key, out var index))
            {
                self._version++;
                return ref self._values[index];
            }

            self.AddValue(key, out index);

            if (shouldBeRecycled.Invoke(ref self._values[index]))
            {
                recycler.Invoke(ref self._values[index], ref parameter);
            }
            else
            {
                self._values[index] = builder.Invoke(ref parameter);
            }

            return ref self._values[index];
        }
    }
}
