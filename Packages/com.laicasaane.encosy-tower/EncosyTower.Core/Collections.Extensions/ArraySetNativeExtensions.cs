using System;
using System.Runtime.CompilerServices;
using EncosyTower.Common;

namespace EncosyTower.Collections.Extensions
{
    public static class ArraySetNativeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> GetItems<T>(this in ArraySetNative<T> self)
            where T : unmanaged, IEquatable<T>
                => self.AsValuesSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetOrAdd<T>(
              this ref ArraySetNative<T> self
            , T value
            , Func<T> builder
        )
            where T : unmanaged, IEquatable<T>
        {
            if (self.AddValue(value, out var index) == false)
            {
                self.BumpVersion();
                return ref self.GetValueRefAt(index);
            }

            self.GetValueRefAt(index) = builder();

            return ref self.GetValueRefAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetOrAdd<T, TParam>(
              this ref ArraySetNative<T> self
            , T value
            , FuncRef<TParam, T> builder
            , ref TParam parameter
        )
            where T : unmanaged, IEquatable<T>
        {
            if (self.AddValue(value, out var index) == false)
            {
                self.BumpVersion();
                return ref self.GetValueRefAt(index);
            }

            self.GetValueRefAt(index) = builder(ref parameter);

            return ref self.GetValueRefAt(index);
        }

        /// <summary>
        /// RecycledOrCreate makes sense to use on sets that are fast cleared and use objects
        /// as value. Once the set is fast cleared, it will try to reuse object values that are
        /// recycled during the fast clearing.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builder"></param>
        /// <param name="recycler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RecycleOrAdd<T>(
              this ref ArraySetNative<T> self
            , T value
            , Func<T> builder
            , ActionRef<T> recycler
            , PredicateRef<T> shouldBeRecycled
        )
            where T : unmanaged, IEquatable<T>
        {
            if (self.AddValue(value, out var index) == false)
            {
                self.BumpVersion();
                return ref self.GetValueRefAt(index);
            }

            if (shouldBeRecycled(ref self.GetValueRefAt(index)))
            {
                recycler(ref self.GetValueRefAt(index));
            }
            else
            {
                self.GetValueRefAt(index) = builder();
            }

            return ref self.GetValueRefAt(index);
        }

        /// <summary>
        /// RecycledOrCreate makes sense to use on sets that are fast cleared and use objects
        /// as value. Once the set is fast cleared, it will try to reuse object values that are
        /// recycled during the fast clearing.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builder"></param>
        /// <param name="recycler"></param>
        /// <param name="parameter"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RecycleOrAdd<T, TParam>(
              this ref ArraySetNative<T> self
            , T value
            , FuncRef<TParam, T> builder
            , ActionRef<T, TParam> recycler
            , PredicateRef<T> shouldBeRecycled
            , ref TParam parameter
        )
            where T : unmanaged, IEquatable<T>
        {
            if (self.AddValue(value, out var index) == false)
            {
                self.BumpVersion();
                return ref self.GetValueRefAt(index);
            }

            if (shouldBeRecycled(ref self.GetValueRefAt(index)))
            {
                recycler(ref self.GetValueRefAt(index), ref parameter);
            }
            else
            {
                self.GetValueRefAt(index) = builder(ref parameter);
            }

            return ref self.GetValueRefAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetOrAdd<T, TBuilder>(
              this ref ArraySetNative<T> self
            , T value
            , ref TBuilder builder
        )
            where T : unmanaged, IEquatable<T>
            where TBuilder : IFunc<T>
        {
            if (self.AddValue(value, out var index) == false)
            {
                self.BumpVersion();
                return ref self.GetValueRefAt(index);
            }

            self.GetValueRefAt(index) = builder.Invoke();

            return ref self.GetValueRefAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetOrAdd<T, TParam, TBuilder>(
              this ref ArraySetNative<T> self
            , T value
            , ref TBuilder builder
            , ref TParam parameter
        )
            where T : unmanaged, IEquatable<T>
            where TBuilder : IFuncRef<TParam, T>
        {
            if (self.AddValue(value, out var index) == false)
            {
                self.BumpVersion();
                return ref self.GetValueRefAt(index);
            }

            self.GetValueRefAt(index) = builder.Invoke(ref parameter);

            return ref self.GetValueRefAt(index);
        }

        /// <summary>
        /// RecycledOrCreate makes sense to use on sets that are fast cleared and use objects
        /// as value. Once the set is fast cleared, it will try to reuse object values that are
        /// recycled during the fast clearing.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builder"></param>
        /// <param name="recycler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RecycleOrAdd<T, TBuilder, TRecyler, TShouldBeRecycled>(
              this ref ArraySetNative<T> self
            , T value
            , ref TBuilder builder
            , ref TRecyler recycler
            , ref TShouldBeRecycled shouldBeRecycled
        )
            where T : unmanaged, IEquatable<T>
            where TBuilder : IFunc<T>
            where TRecyler : IActionRef<T>
            where TShouldBeRecycled : IPredicateRef<T>
        {
            if (self.AddValue(value, out var index) == false)
            {
                self.BumpVersion();
                return ref self.GetValueRefAt(index);
            }

            if (shouldBeRecycled.Invoke(ref self.GetValueRefAt(index)))
            {
                recycler.Invoke(ref self.GetValueRefAt(index));
            }
            else
            {
                self.GetValueRefAt(index) = builder.Invoke();
            }

            return ref self.GetValueRefAt(index);
        }

        /// <summary>
        /// RecycledOrCreate makes sense to use on sets that are fast cleared and use objects
        /// as value. Once the set is fast cleared, it will try to reuse object values that are
        /// recycled during the fast clearing.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builder"></param>
        /// <param name="recycler"></param>
        /// <param name="parameter"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RecycleOrAdd<T, TParam, TBuilder, TRecyler, TShouldBeRecycled>(
              this ref ArraySetNative<T> self
            , T value
            , ref TBuilder builder
            , ref TRecyler recycler
            , ref TShouldBeRecycled shouldBeRecycled
            , ref TParam parameter
        )
            where T : unmanaged, IEquatable<T>
            where TBuilder : IFuncRef<TParam, T>
            where TRecyler : IActionRef<T, TParam>
            where TShouldBeRecycled : IPredicateRef<T>
        {
            if (self.AddValue(value, out var index) == false)
            {
                self.BumpVersion();
                return ref self.GetValueRefAt(index);
            }

            if (shouldBeRecycled.Invoke(ref self.GetValueRefAt(index)))
            {
                recycler.Invoke(ref self.GetValueRefAt(index), ref parameter);
            }
            else
            {
                self.GetValueRefAt(index) = builder.Invoke(ref parameter);
            }

            return ref self.GetValueRefAt(index);
        }
    }
}
