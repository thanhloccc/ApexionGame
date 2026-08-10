#if UNITASK || UNITY_6000_0_OR_NEWER

using System;

namespace EncosyTower.Persistences
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class PersistAccessorAttribute : Attribute
    {
        public PersistAccessorAttribute(Type persistenceType)
        {
            PersistenceType = persistenceType;
        }

        public Type PersistenceType { get; }
    }
}

#endif
