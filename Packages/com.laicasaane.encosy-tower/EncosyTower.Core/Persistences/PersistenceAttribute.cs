#if UNITASK || UNITY_6000_0_OR_NEWER

using System;

namespace EncosyTower.Persistences
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PersistenceAttribute : Attribute
    {
    }
}

#endif
