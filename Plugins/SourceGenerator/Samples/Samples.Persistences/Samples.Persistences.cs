using System;
using System.ComponentModel;
using EncosyTower.Initialization;
using EncosyTower.Persistences;

namespace Samples.Persistences
{
    [Persist]
    internal partial record class CommonData
    {
        [property: JsonIgnore]
        private string _id;
    }

    [Persist]
    internal partial class ProgressData { }

    [Persist]
    internal partial class UserEventData { }

    [DisplayName("Common")]
    [PersistAccessor(typeof(CommonPersistence))]
    public sealed class CommonDataAccessor : IPersistAccessor, IInitializable, IDeinitializable
    {
        internal CommonDataAccessor(PersistStoreDefault<CommonData> storage) { }

        public void Deinitialize()
        {
        }

        public void Initialize()
        {
        }
    }

    [DisplayName("Progress")]
    [PersistAccessor(typeof(ProgressPersistence))]
    public sealed class ProgressDataAccessor : IPersistAccessor, IInitializable, IDeinitializable
    {
        internal ProgressDataAccessor(PersistStoreDefault<ProgressData> storage) { }

        public void Deinitialize()
        {
        }

        public void Initialize()
        {
        }
    }

    [DisplayName("Event")]
    [PersistAccessor(typeof(ProgressPersistence))]
    public sealed class UserEventDataAccessor : IPersistAccessor, IInitializable, IDeinitializable
    {
        internal UserEventDataAccessor(PersistStoreDefault<UserEventData> storage) { }

        public void Deinitialize()
        {
        }

        public void Initialize()
        {
        }
    }

    [Persistence]
    internal static partial class CommonPersistence
    {
        partial class PersistDirectory
        {
            static partial void GetIgnoreEncryption(ref bool ignoreEncryption)
            {
            }

            private static partial PersistStoreArgs GetStoreArgs<TData, TStore>(Func<TData> createDataFunc)
                where TData : IPersist
                where TStore : PersistStoreBase<TData>
            {
                return default;
            }
        }
    }

    [Persistence]
    public static partial class ProgressPersistence
    {
        partial class PersistDirectory
        {
            static partial void GetIgnoreEncryption(ref bool ignoreEncryption)
            {
            }

            private static partial PersistStoreArgs GetStoreArgs<TData, TStore>(Func<TData> createDataFunc)
                where TData : IPersist
                where TStore : PersistStoreBase<TData>
            {
                return default;
            }
        }
    }
}
