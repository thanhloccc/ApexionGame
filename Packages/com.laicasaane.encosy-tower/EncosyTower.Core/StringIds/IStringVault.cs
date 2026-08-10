using System;
using EncosyTower.Collections;
using EncosyTower.Common;

namespace EncosyTower.StringIds
{
    public interface IStringVault
        : IReadOnlyStringVault
        , IDisposable
        , IClearable
        , IIncreaseCapacity
    {
        StringId GetOrMakeId(in UnmanagedString str);
    }

    public interface IReadOnlyStringVault
        : IIsCreated
        , IHasCapacity
        , IHasCount
        , ICopyToSpan<UnmanagedString>
        , ITryCopyToSpan<UnmanagedString>
    {
        bool AllowEmptyString { get; }

        Option<StringId> TryGetId(in UnmanagedString str);

        bool TryGetId(in UnmanagedString str, out StringId result);

        Option<UnmanagedString> TryGetUnmanagedString(StringId id);

        bool TryGetUnmanagedString(StringId id, out UnmanagedString result);

        bool ContainsId(StringId id);
    }
}
