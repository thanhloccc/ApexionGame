// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace EncosyTower.Debugging
{
    /// <summary>
    /// Provides generic exception factories for Encosy Tower validation.
    /// </summary>
    /// <remarks>
    /// Conditional guard symbols are declared by <see cref="ValidationDefines"/>. Area-specific
    /// guards are provided by their corresponding ThrowHelper classes.
    /// </remarks>
    public static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException CreateInvalidOperationException_TypeNotCreatedCorrectly(string name)
            => new($"Type '{name}' was not created correctly.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentNullException CreateArgumentNullException(string paramName)
            => new(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException_LengthNegative()
            => new("length", "The value must be non-negative.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException_IndexNegative()
            => new("index", "The value must be non-negative.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException CreateArgumentException_ArrayPlusOffTooSmall()
            => new("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
    }
}
