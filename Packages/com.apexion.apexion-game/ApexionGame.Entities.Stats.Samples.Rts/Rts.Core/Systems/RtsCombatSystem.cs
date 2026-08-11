using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Swings, damage and healing. The only stat this system writes is <c>Hp</c>.
    /// </summary>
    /// <remarks>
    /// Everything that makes a unit hit harder, faster or tougher lives in the graph, not here: research on
    /// the team node, a hero's aura on the same node, a Bloodlust modifier on the unit. This file just reads
    /// the settled numbers.
    /// </remarks>
    public sealed class RtsCombatSystem
    {
        private readonly RtsMatchContext _context;
        private readonly RtsMovementSystem _movement;

        public RtsCombatSystem(RtsMatchContext context, RtsMovementSystem movement)
        {
            _context = context;
            _movement = movement;
        }

        /// <summary>Armour as damage reduction: 20 armour takes 83% of a swing.</summary>
        public static float Mitigation(float armor) => 100f / (100f + math.max(armor, 0f));

        public void Tick(float deltaTime)
        {
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                var units = teams[t].Roster.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    Fight(units[i], deltaTime);
                }
            }
        }

        private void Fight(RtsUnit unit, float deltaTime)
        {
            if (unit.Alive == false || unit.IsStructure)
            {
                return;
            }

            unit.Cooldown -= deltaTime;

            if (_movement.InRange(unit) == false)
            {
                // Walking does not bank swings: a unit that spent ten seconds crossing the field would
                // otherwise arrive with a fistful of free attacks.
                unit.Cooldown = math.max(unit.Cooldown, 0f);
                return;
            }

            // The loop advances by `interval` per swing, so a near-zero interval would spin forever. The
            // unit's ClampMin modifier already keeps the stat above the floor; this math.max is what saves
            // the loop if that modifier ever failed to attach.
            var interval = math.max(
                  _context.Read(unit.Handles.attackInterval)
                , _context.Settings.attackIntervalFloor);

            while (unit.Cooldown <= 0f)
            {
                Swing(unit, unit.Target);
                unit.Cooldown += interval;

                if (_context.Read(unit.Target.Handles.hp) <= 0f)
                {
                    break;
                }
            }
        }

        private void Swing(RtsUnit attacker, RtsUnit target)
        {
            var attack = _context.Read(attacker.Handles.attack);

            if (attack <= 0f)
            {
                return;
            }

            var variance = _context.Settings.damageVariance;
            var roll = 1f + _context.NextFloat(-variance, variance);
            var mitigation = Mitigation(_context.Read(target.Handles.armor));

            _context.Battle.Swing(attacker.Id, target.Id);

            Damage(target, attack * roll * mitigation, attacker);
        }

        /// <summary>Takes health off a unit by writing its <c>Hp</c> base value.</summary>
        public void Damage(RtsUnit unit, float amount, RtsUnit source = null)
        {
            if (amount <= 0f || unit.Alive == false)
            {
                return;
            }

            var hp = math.max(_context.Read(unit.Handles.hp) - amount, 0f);

            _context.World.Write(unit.Handles.hp, hp);
            _context.Battle.Hit(unit.Id, amount);

            if (source != null)
            {
                unit.LastAttacker = source;
            }
        }

        /// <summary>
        /// Writes <c>Hp</c> upwards and lets the unit's <c>ClampMax</c> modifier do the capping.
        /// </summary>
        /// <remarks>
        /// Note what is missing: any mention of <c>MaxHp</c>. Over-healing is the graph's problem.
        /// </remarks>
        public void Heal(RtsUnit unit, float amount)
        {
            if (amount <= 0f || unit.Alive == false)
            {
                return;
            }

            _context.World.Write(unit.Handles.hp, _context.Read(unit.Handles.hp) + amount);
        }
    }
}
