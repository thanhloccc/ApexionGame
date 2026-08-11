using System;
using EncosyTower.Collections;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    public enum RtsBattleEventKind : byte
    {
        Swing,
        Hit,
        Death,
    }

    /// <summary>Something the view might want to animate. Ids, not object references.</summary>
    public readonly record struct RtsBattleEvent(
          RtsBattleEventKind Kind
        , int UnitId
        , int OtherUnitId
        , float Amount
    );

    /// <summary>
    /// What happened during the ticks the view has not drawn yet.
    /// </summary>
    /// <remarks>
    /// This is how flash timers and hit sparks stay out of <see cref="RtsUnit"/>. The simulation pushes
    /// facts; the presentation layer decides what a fact looks like and how long it lingers. A frame can
    /// contain several ticks, so the feed accumulates until the view drains it.
    /// </remarks>
    public sealed class RtsBattleFeed
    {
        private readonly FasterList<RtsBattleEvent> _events;

        public RtsBattleFeed(int capacity = 256)
        {
            _events = new FasterList<RtsBattleEvent>(capacity);
        }

        public ReadOnlySpan<RtsBattleEvent> Events => _events.AsReadOnlySpan();

        public void Swing(int attackerId, int targetId)
            => _events.Add(new RtsBattleEvent(RtsBattleEventKind.Swing, attackerId, targetId, 0f));

        public void Hit(int unitId, float amount)
            => _events.Add(new RtsBattleEvent(RtsBattleEventKind.Hit, unitId, 0, amount));

        public void Death(int unitId, int killerId)
            => _events.Add(new RtsBattleEvent(RtsBattleEventKind.Death, unitId, killerId, 0f));

        public void Clear() => _events.Clear();
    }
}
