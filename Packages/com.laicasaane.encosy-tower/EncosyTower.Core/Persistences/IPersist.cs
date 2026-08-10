#if UNITASK || UNITY_6000_0_OR_NEWER

namespace EncosyTower.Persistences
{
    public interface IPersist
    {
        string Id { get; set; }

        int Version { get; set; }
    }
}

#endif
