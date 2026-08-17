using ApexionGame.HFSM;
using EncosyTower.Common;
using Game.Common;
using Game.Data;

namespace Game.Gameplay.Weapons
{
    public static class RangedFireMachine
    {
        public const string MachineName = "RangedFire";

        public static Success<WeaponError> Validate(ItemId weaponId, in RangedWeaponData weapon)
        {
            if (weapon.RoundsPerMinute <= 0f)
            {
                return WeaponError.NonPositiveRateOfFire(weaponId, weapon.RoundsPerMinute);
            }

            if (weapon.MagazineCapacity <= 0)
            {
                return WeaponError.NonPositiveMagazine(weaponId, weapon.MagazineCapacity);
            }

            return Success.Yes;
        }

        public static Result<
              MachineDefinition<RangedWeaponContext, RangedFireState>
            , WeaponError
        > BuildOrError()
        {
            var result = HierarchicalStateMachine<RangedWeaponContext, RangedFireState>
                .Define(MachineName)
                .State<RangedIdleState>(RangedFireState.Idle)
                    .To(RangedFireState.Firing).On(WeaponTrigger.Attack)
                    .To(RangedFireState.Reloading).On(WeaponTrigger.Reload)
                .State<RangedFiringState>(RangedFireState.Firing)
                    .To(RangedFireState.Empty)
                        .When(static c => c.Rounds < 1)
                        .Priority(2)
                    .To(RangedFireState.Cycling)
                        .When(static _ => true)
                .State(RangedFireState.Cycling)
                    .To(RangedFireState.Firing)
                        .When(static (c, i) => i.TimeInState >= c.FireInterval && c.WantsAnotherShot)
                        .Priority(2)
                    .To(RangedFireState.Idle)
                        .When(static (c, i) => i.TimeInState >= c.FireInterval)
                .State<RangedEmptyState>(RangedFireState.Empty)
                    .To(RangedFireState.Reloading).On(WeaponTrigger.Reload)
                .State<RangedReloadingState>(RangedFireState.Reloading)
                    .To(RangedFireState.Idle)
                        .When(static (c, i) => i.TimeInState >= c.ReloadSeconds)
                        .Do(static c => c.Refill())
                .AnyState()
                    .To(RangedFireState.Idle).On(WeaponTrigger.Cancel).Priority(3)
                .BuildOrError();

            if (result.TryGetValue(out var definition))
            {
                return definition;
            }

            return WeaponError.MachineBuildFailed(default);
        }
    }

    internal sealed class RangedIdleState : StateBehaviour<RangedWeaponContext>
    {
        protected override void OnEnter(RangedWeaponContext context, in StateInfo info)
            => context.BeginBurst();
    }

    internal sealed class RangedFiringState : StateBehaviour<RangedWeaponContext>
    {
        protected override void OnEnter(RangedWeaponContext context, in StateInfo info)
            => context.ConsumeRound();
    }

    internal sealed class RangedEmptyState : StateBehaviour<RangedWeaponContext>
    {
        protected override void OnEnter(RangedWeaponContext context, in StateInfo info)
            => context.Emit(WeaponEventKind.WentEmpty, 0f);
    }

    internal sealed class RangedReloadingState : StateBehaviour<RangedWeaponContext>
    {
        protected override void OnEnter(RangedWeaponContext context, in StateInfo info)
            => context.Emit(WeaponEventKind.ReloadStarted, 0f);
    }
}
