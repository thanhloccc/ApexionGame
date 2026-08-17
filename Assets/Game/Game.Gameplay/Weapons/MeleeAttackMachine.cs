using ApexionGame.HFSM;
using EncosyTower.Common;

namespace Game.Gameplay.Weapons
{
    public static class MeleeAttackMachine
    {
        public const string MachineName = "MeleeAttack";

        public static Result<
              MachineDefinition<MeleeWeaponContext, MeleeAttackState>
            , WeaponError
        > BuildOrError()
        {
            var result = HierarchicalStateMachine<MeleeWeaponContext, MeleeAttackState>
                .Define(MachineName)
                .State(MeleeAttackState.Idle)
                    .To(MeleeAttackState.WindUp).On(WeaponTrigger.Attack)
                .State(MeleeAttackState.WindUp)
                    .To(MeleeAttackState.Active)
                        .When(static (c, i) => i.TimeInState >= c.WindUpSeconds)
                .State<MeleeActiveState>(MeleeAttackState.Active)
                    .To(MeleeAttackState.Recovery)
                        .When(static (c, i) => i.TimeInState >= c.ActiveSeconds)
                .State(MeleeAttackState.Recovery)
                    .To(MeleeAttackState.Idle)
                        .When(static (c, i) => i.TimeInState >= c.RecoverySeconds)
                .AnyState()
                    .To(MeleeAttackState.Idle).On(WeaponTrigger.Cancel)
                .BuildOrError();

            if (result.TryGetValue(out var definition))
            {
                return definition;
            }

            return WeaponError.MachineBuildFailed(default);
        }
    }

    internal sealed class MeleeActiveState : StateBehaviour<MeleeWeaponContext>
    {
        protected override void OnEnter(MeleeWeaponContext context, in StateInfo info)
            => context.Emit(WeaponEventKind.AttackActivated, context.BaseDamage);
    }
}
