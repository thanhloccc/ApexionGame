using EncosyTower.EnumExtensions;

namespace ApexionGame.HFSM.Samples;

/// <summary>
/// The node vocabulary for <see cref="EnemyBrain"/>. <c>Idle</c>/<c>Patrol</c>/<c>Flee</c> are
/// top-level leaves, <c>Combat</c> is the composite over <c>Chase</c>/<c>Attack</c>, and
/// <c>Stagger</c> is the one async leaf (button 8, gated on <c>UNITASK</c>).
/// </summary>
public enum EnemyState
{
    Idle,
    Patrol,
    Combat,
    Chase,
    Attack,
    Flee,
    Stagger,
}

[EnumExtensionsFor(typeof(EnemyState))]
public static partial class EnemyStateExtensions { }

/// <summary>
/// The event vocabulary <see cref="EnemyBrain"/> reacts to via <c>.On(...)</c>, as opposed to the
/// polled guards it reacts to via <c>.When(...)</c>.
/// </summary>
public enum EnemyTrigger
{
    AttackFinished,
    Healed,
    Staggered,
}

[EnumExtensionsFor(typeof(EnemyTrigger))]
public static partial class EnemyTriggerExtensions { }
