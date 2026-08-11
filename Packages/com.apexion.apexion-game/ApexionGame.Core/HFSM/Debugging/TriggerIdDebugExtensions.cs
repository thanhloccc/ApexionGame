using System;
using EncosyTower.Types;

namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// Resolves a <see cref="TriggerId"/> back to the enum member name it was interned from, for
    /// tooling only — the runtime never needs the name, only the id.
    /// </summary>
    public static class TriggerIdDebugExtensions
    {
        /// <summary>
        /// The trigger's declared member name (<c>"AttackFinished"</c>), or a numbered fallback when
        /// the owning enum's <see cref="Type"/> was not resolvable.
        /// </summary>
        public static string ToDisplayName(this TriggerId trigger)
        {
            if (trigger.IsValid == false)
            {
                return "none";
            }

            if (trigger.Type.TryToType(out var enumType) == false)
            {
                return $"#{trigger.Value}";
            }

            try
            {
                return Enum.ToObject(enumType, trigger.Value).ToString();
            }
            catch (ArgumentException)
            {
                return $"#{trigger.Value}";
            }
        }
    }
}
