using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Picks a target, walks towards it, and keeps a melee blob from standing inside itself.
    /// </summary>
    /// <remarks>
    /// The only stats it reads are <c>MoveSpeed</c> and <c>AttackRange</c>, and it writes none — so a Boots
    /// research or a Slow debuff changes how the army moves without a single line here knowing they exist.
    /// </remarks>
    public sealed class RtsMovementSystem
    {
        /// <summary>
        /// How much further away a stronghold has to look before a unit prefers it over a soldier.
        /// </summary>
        private const float StructurePenalty = 6f;

        private readonly RtsMatchContext _context;

        public RtsMovementSystem(RtsMatchContext context)
        {
            _context = context;
        }

        public void Tick(float deltaTime)
        {
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                Advance(teams[t], _context.EnemyOf(t), deltaTime);
            }

            Separate();
            ClampToField();
        }

        private void Advance(RtsTeam team, RtsTeam enemy, float deltaTime)
        {
            var units = team.Roster.AsReadOnlySpan();

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];

                unit.PreviousPosition = unit.Position;

                if (unit.Alive == false || unit.IsStructure)
                {
                    continue;
                }

                unit.Target = AcquireTarget(unit, enemy);

                if (unit.Target == null || InRange(unit))
                {
                    continue;
                }

                var toTarget = unit.Target.Position - unit.Position;
                var speed = _context.Read(unit.Handles.moveSpeed);

                if (speed > 0f && math.lengthsq(toTarget) > 1e-5f)
                {
                    unit.Position += math.normalize(toTarget) * speed * deltaTime;
                }
            }
        }

        /// <summary>True when the unit can hit whatever it is aiming at from where it stands.</summary>
        public bool InRange(RtsUnit unit)
        {
            if (unit.Target == null)
            {
                return false;
            }

            var range = math.max(_context.Read(unit.Handles.attackRange), 0.2f);

            return RtsGeometry.Gap(unit, unit.Target) <= range;
        }

        /// <remarks>
        /// Nearest living enemy, with structures pushed to the back of the queue so an army walking past a
        /// stronghold still prefers the soldiers defending it. Nothing in range means "advance on the enemy
        /// stronghold", which is what ends a stalled match.
        /// </remarks>
        private RtsUnit AcquireTarget(RtsUnit unit, RtsTeam enemy)
        {
            var candidates = enemy.Roster.AsReadOnlySpan();
            var bestDistance = _context.Settings.acquireRange;

            RtsUnit best = null;

            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];

                if (candidate.Alive == false)
                {
                    continue;
                }

                var distance = RtsGeometry.Gap(unit.Position, candidate);

                if (candidate.IsStructure)
                {
                    distance += StructurePenalty;
                }

                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best ?? enemy.Stronghold;
        }

        /// <remarks>
        /// O(n²) within a team, which is fine at the army cap this sample uses. It exists so a melee blob
        /// reads as a crowd rather than as one unit standing in eleven others.
        /// </remarks>
        private void Separate()
        {
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                var units = teams[t].Roster.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    var a = units[i];

                    for (var j = i + 1; j < units.Length; j++)
                    {
                        var b = units[j];

                        if (a.IsStructure && b.IsStructure)
                        {
                            continue;
                        }

                        Push(a, b);
                    }
                }
            }
        }

        private static void Push(RtsUnit a, RtsUnit b)
        {
            var delta = b.Position - a.Position;
            var minimum = a.Archetype.Radius + b.Archetype.Radius;
            var squared = math.lengthsq(delta);

            if (squared > minimum * minimum)
            {
                return;
            }

            float2 direction;
            float push;

            if (squared < 1e-6f)
            {
                // Exactly on top of each other. A fixed direction rather than a random one, so a replay of
                // the same inputs stays a replay.
                direction = new float2(1f, 0f);
                push = minimum * 0.5f;
            }
            else
            {
                var distance = math.sqrt(squared);
                direction = delta / distance;
                push = (minimum - distance) * 0.5f;
            }

            if (a.IsStructure == false)
            {
                a.Position -= direction * push;
            }

            if (b.IsStructure == false)
            {
                b.Position += direction * push;
            }
        }

        private void ClampToField()
        {
            var limit = new float2(
                  _context.Settings.fieldHalfLength + 2f
                , _context.Settings.laneHalfWidth);

            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                var units = teams[t].Roster.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    var unit = units[i];

                    if (unit.IsStructure == false)
                    {
                        unit.Position = math.clamp(unit.Position, -limit, limit);
                    }
                }
            }
        }
    }
}
