#if UNITASK || UNITY_6000_0_OR_NEWER

using System.Collections.Generic;
using EncosyTower.Collections;

namespace EncosyTower.Persistences
{
    public interface IPersistCollection : IReadOnlyList<IPersist>
        , ICopyToSpan<IPersist>, ITryCopyToSpan<IPersist>
    {
    }
}

#endif
