#if UNITASK || UNITY_6000_0_OR_NEWER

namespace EncosyTower.Persistences
{
    public enum SourcePriority : byte
    {
        RemoteThenDevice,
        DeviceThenRemote,
        OnlyRemote,
        OnlyDevice,
    }
}

#endif
