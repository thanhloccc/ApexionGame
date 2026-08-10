// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    /// <summary>
    /// Provides exception helpers for collection and buffer validation.
    /// </summary>
    /// <remarks>
    /// Conditional guards use the collection symbols declared by
    /// <see cref="EncosyTower.Debugging.ValidationDefines"/>.
    /// </remarks>
    public static class ThrowHelper
    {
        public enum CollectionType
        {
            Unknown = 0,
            ArrayMap,
            ArrayMapNative,
            ArrayMapUnsafe,
            ArrayMapUnsafeReadOnly,
            ArraySet,
            ArraySetNative,
            ArraySetUnsafe,
            ArraySetUnsafeReadOnly,
            ArrayUnsafe,
            FasterList,
            ListFast,
            ListNative,
            ListNativeReadOnly,
            ListProxy,
            ListProxyReadOnly,
            ListUnsafe,
            ListUnsafeReadOnly,
            ReferenceUnsafe,
            SharedArray,
            SharedArrayMap,
            SharedArrayMapReadOnly,
            SharedArrayMapNative,
            SharedArrayMapNativeReadOnly,
            SharedArrayMapUnsafe,
            SharedList,
            SharedListWithNative,
            SharedListWithNativeReadOnly,
            SharedListNative,
            SharedListNativeReadOnly,
            SharedListUnsafe,
            SharedQueue,
            SharedQueueUnsafe,
            SharedStack,
            SharedStackUnsafe,
            QueueUnsafe,
            StackUnsafe,
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCollectionTypeName(CollectionType type)
            => type switch {
                CollectionType.ArrayMap => "ArrayMap<TKey, TValue>",
                CollectionType.ArrayMapNative => "ArrayMapNative<TKey, TValue>",
                CollectionType.ArrayMapUnsafe => "ArrayMapUnsafe<TKey, TValue>",
                CollectionType.ArrayMapUnsafeReadOnly => "ArrayMapUnsafe<TKey, TValue>.ReadOnly",
                CollectionType.ArraySet => "ArraySet<T>",
                CollectionType.ArraySetNative => "ArraySetNative<T>",
                CollectionType.ArraySetUnsafe => "ArraySetUnsafe<T>",
                CollectionType.ArraySetUnsafeReadOnly => "ArraySetUnsafe<T>.ReadOnly",
                CollectionType.ArrayUnsafe => "ArrayUnsafe<T>",
                CollectionType.FasterList => "FasterList<T>",
                CollectionType.ListFast => "ListFast<T>",
                CollectionType.ListNative => "ListNative<T>",
                CollectionType.ListNativeReadOnly => "ListNative<T>.ReadOnly",
                CollectionType.ListProxy => "ListProxy<TProvider, TBuffer, T>",
                CollectionType.ListProxyReadOnly => "ListProxy<TProvider, TBuffer, T>.ReadOnly",
                CollectionType.ListUnsafe => "ListUnsafe<T>",
                CollectionType.ListUnsafeReadOnly => "ListUnsafe<T>.ReadOnly",
                CollectionType.ReferenceUnsafe => "ReferenceUnsafe<T>",
                CollectionType.SharedArray => "SharedArray<T, TNative>",
                CollectionType.SharedArrayMap => "SharedArrayMap<TKey, TValue, TValueNative>",
                CollectionType.SharedArrayMapReadOnly => "SharedArrayMap<TKey, TValue, TValueNative>.ReadOnly",
                CollectionType.SharedArrayMapNative => "SharedArrayMapNative<TKey, TValue>",
                CollectionType.SharedArrayMapNativeReadOnly => "SharedArrayMapNative<TKey, TValue>.ReadOnly",
                CollectionType.SharedArrayMapUnsafe => "SharedArrayMapUnsafe<TKey, TValue>",
                CollectionType.SharedList => "SharedList<T>",
                CollectionType.SharedListWithNative => "SharedList<T, TNative>",
                CollectionType.SharedListWithNativeReadOnly => "SharedList<T, TNative>.ReadOnly",
                CollectionType.SharedListNative => "SharedListNative<T>",
                CollectionType.SharedListNativeReadOnly => "SharedListNative<T>.ReadOnly",
                CollectionType.SharedListUnsafe => "SharedListUnsafe<T>",
                CollectionType.SharedQueue => "SharedQueue<T, TNative>",
                CollectionType.SharedQueueUnsafe => "SharedQueueUnsafe<T>",
                CollectionType.SharedStack => "SharedStack<T, TNative>",
                CollectionType.SharedStackUnsafe => "SharedStackUnsafe<T>",
                CollectionType.QueueUnsafe => "QueueUnsafe<T>",
                CollectionType.StackUnsafe => "StackUnsafe<T>",
                _ => "collection",
            };

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfInvalidAllocatorStrategy(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new(
                    "Allocator strategy must be either Unity.Collections.Allocator " +
                    "or Unity.Collections.AllocatorManager.AllocatorHandle"
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfBufferAlreadyAllocated(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Cannot allocate an already allocated buffer.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfResizeUninitializedBuffer(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Cannot resize an uninitialized buffer.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfTypesNotEqualSize<T, U>(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new(
                    $"size of type '{typeof(U).FullName}' must be equal to " +
                    $"size of type '{typeof(T).FullName}'"
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfBufferShiftIndexIsOutOfRange(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Out of bounds index");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfBufferShiftCountIsOutOfRange(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Out of bounds count");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfBufferShiftCountIsBelowIndex(
            [DoesNotReturnIf(false)] bool valid
        )
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Count is lesser than index");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfBucketsAreUninitialized(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new(type switch {
                    CollectionType.ArraySet => "Set arrays are not correctly initialized (0 size).",
                    CollectionType.ArraySetUnsafe => "Set arrays are not correctly initialized (0 size).",
                    CollectionType.ArraySetUnsafeReadOnly => "Set arrays are not correctly initialized (0 size).",
                    CollectionType.SharedArrayMap => "Map arrays are not correctly initialized (0 size)",
                    CollectionType.SharedArrayMapNative => "Map arrays are not correctly initialized (0 size)",
                    _ => "Map arrays are not correctly initialized (0 size).",
                });
        }

        [HideInCallstack, StackTraceHidden]
        public static void ThrowIfSourceCollectionIsNotCreated(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentException CreateException(CollectionType type)
                => new(
                    type switch {
                        CollectionType.ArraySet => "The source set is not created.",
                        CollectionType.ArraySetNative => "The source set is not created.",
                        CollectionType.ArraySetUnsafe => "The source set is not created.",
                        _ => "The source map is not created.",
                    },
                    "source"
                );
        }

        [HideInCallstack, StackTraceHidden]
        public static void ThrowIfNativeSourceCollectionIsNotCreated(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new(type switch {
                    CollectionType.ArraySetNative => "The source set is not created.",
                    _ => "The source map is not created.",
                });
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfCapacityIsImmutable(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new($"the capacity of {GetCollectionTypeName(type)} is immutable and cannot change");
        }

        [HideInCallstack, StackTraceHidden]
        public static void ThrowIfUnsafeCollectionIsDisposed(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ObjectDisposedException CreateException(CollectionType type)
                => new(null, $"The {GetCollectionTypeName(type)} is already disposed.");
        }

        [HideInCallstack, StackTraceHidden]
        public static void ThrowIfUnsafeCollectionAllocatorIsInvalid(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new(
                    $"The {GetCollectionTypeName(type)} can not be Disposed because it was not allocated "
                    + "with a valid allocator."
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfUnsafeCollectionTypeIsManaged<T>(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new(
                    $"{typeof(T)} used in {GetCollectionTypeName(type)} must be unmanaged "
                    + "(contain no managed types)."
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(
            [DoesNotReturnIf(false)] bool valid
        )
            where T : unmanaged
            where TNative : unmanaged
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new(
                    $"size of native alias type '{typeof(TNative).FullName}' " +
                    $"({UnsafeUtility.SizeOf<TNative>()} bytes) " +
                    $"must be equal to size of source type '{typeof(T).FullName}' ({UnsafeUtility.SizeOf<T>()} bytes)"
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfAllocatorIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("allocator is invalid");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfCapacityIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("capacity must be non-negative");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfCapacityBelowCount([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("new capacity cannot be smaller than count");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfSourceStartIndexIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("sourceStartIndex is outside the range of valid indexes");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfSourceLengthIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("length is outside the source range");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfSourceCountIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("count is outside the source range");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfDestinationLengthIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("destination is shorter than the requested length");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfIndexIsNegative([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("index is less than 0");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfCountIsNegative([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("count is less than 0");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfArgumentIndexIsNegative([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException()
                => new("index", "Index must be non-negative.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfArgumentCountIsNegative([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException()
                => new("count", "Count must be non-negative.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfRemovalIndexIsOutOfRange([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("out of bound index");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfRemovalRangeIsOutOfRange([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("out of bound length");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfStartIndexIsOutOfRange([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("out of bound start index");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfNewCapacityIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("newSize is not greater than the current capacity");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfAmountIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Amount must be greater than 0.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfTypesHaveDifferentSize([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("T and U must have equal size");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfEmpty(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new(type switch {
                    CollectionType.ListUnsafe => "the list is empty",
                    CollectionType.QueueUnsafe => "the queue is empty",
                    CollectionType.SharedQueue => "the queue is empty",
                    CollectionType.SharedQueueUnsafe => "the queue is empty",
                    CollectionType.StackUnsafe => "the stack is empty",
                    CollectionType.SharedStack => "the stack is empty",
                    CollectionType.SharedStackUnsafe => "the stack is empty",
                    _ => $"the {GetCollectionTypeName(type)} is empty",
                });
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfIndexIsOutOfRange(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new($"index is outside the range of valid indexes for the {GetCollectionTypeName(type)}");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfInsertionIndexIsOutOfRange(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new($"index is outside the range of valid indexes for the {GetCollectionTypeName(type)}");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfIndexSectionIsInvalid(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new($"index and count do not specify a valid section in the {GetCollectionTypeName(type)}");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfFindStartIndexIsOutOfRange(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new($"startIndex is outside the range of valid indexes for the {GetCollectionTypeName(type)}");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfFindSectionIsInvalid(
              [DoesNotReturnIf(false)] bool valid
            , CollectionType type
        )
        {
            if (valid == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(CollectionType type)
                => new($"startIndex and count do not specify a valid section in the {GetCollectionTypeName(type)}");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfNotUnmanagedType<T>([DoesNotReturnIf(false)] bool isUnmanaged)
        {
            if (isUnmanaged == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new($"{typeof(T)} is not an unmanaged type. Only unmanaged type is supported.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfIndexOutOfRangeException([DoesNotReturnIf(false)] bool withinRange)
        {
            if (withinRange == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static IndexOutOfRangeException CreateException()
                => new("Index must be non-negative and less than the size of the buffer.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowArgumentOutOfRangeException_IfNegative(int value, string paramName)
        {
            if (value < 0)
            {
                throw CreateException(paramName);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException(string paramName)
                => new(paramName, "The value must be non-negative.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowArgumentOutOfRangeException_IfNegativeZero(int value, string paramName)
        {
            if (value <= 0)
            {
                throw CreateException(paramName);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException(string paramName)
                => new(paramName, "The value must be positive and non-zero.");
        }

        [StackTraceHidden, HideInCallstack, DoesNotReturn]
        public static void ThrowArgumentException_ArrayPlusOffTooSmall()
            => throw EncosyTower.Debugging.ThrowHelper.CreateArgumentException_ArrayPlusOffTooSmall();

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowInvalidOperationException_ReadOnlyCollectionNotCreated(
            [DoesNotReturnIf(false)] bool isCreated
        )
        {
            if (isCreated == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("The read-only collection is not created.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfCollectionWasModified([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Collection was modified after the enumerator was instantiated.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfEnumeratorOperationIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Enumeration has either not started or has already finished.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfEnumeratorIsInvalid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Enumerator is not retrieved via a valid method.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfKeyIsPresent([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Key already present");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfMapIsBeingIterated([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Cannot modify a map while it is being iterated");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfSetIsBeingIterated([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Cannot modify a set while it is being iterated");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfSetCountExceedsStartingCount([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Cannot set a count greater than the starting one");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        public static void ThrowIfKeyIsNotFound([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static KeyNotFoundException CreateException()
                => new("Key not found");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IndexOutOfRangeException CreateIndexOutOfRangeException_Collection()
            => new("Index was out of range. Must be non-negative and less than the size of the collection.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException CreateInvalidOperationException_CollectionNotCreated()
            => new("Collection was not created.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException CreateArgumentException_CollectionNotCreated(string paramName)
            => new("Collection was not created.", paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException CreateArgumentException_SourceStartIndex_Length()
            => new("The number of elements from 'sourceStartIndex' to the end of the collection is lesser than 'length'.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException CreateArgumentException_DestinationTooShort()
            => new("The destination span is too short to copy the requested number of elements.");
    }
}
