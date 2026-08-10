using Unity.Mathematics;
using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// The player's side of the game: what is selected, which spell is being aimed, and every verb the HUD and
    /// the keyboard share.
    /// </summary>
    /// <remarks>
    /// Both input paths land here rather than in the MonoBehaviour, so a key and its button cannot drift apart.
    /// The controller never touches the store — it only calls the match's verbs and keeps the refusal message
    /// that came back.
    /// </remarks>
    public sealed class RtsPlayerController
    {
        private const float NoticeSeconds = 2.5f;
        private const float PickRadius = 1.4f;

        private readonly int _teamIndex;

        private float _noticeTimer;
        private string _notice = string.Empty;

        public RtsPlayerController(int teamIndex)
        {
            _teamIndex = teamIndex;
        }

        public int TeamIndex => _teamIndex;

        public RtsUnit Selected { get; private set; }

        public bool InspectingNode { get; private set; }

        /// <summary>Index of the spell being aimed, or -1.</summary>
        public int AimingSpell { get; private set; } = -1;

        public string Notice => _noticeTimer > 0f ? _notice : string.Empty;

        public void Tick(float deltaTime)
        {
            if (_noticeTimer > 0f)
            {
                _noticeTimer -= deltaTime;
            }

            if (Selected is { Alive: false })
            {
                Selected = null;
            }
        }

        // ---- input -----------------------------------------------------------------------------

        public void HandleKeys(RtsMatch match, RtsInput input, RtsMatchLoop loop)
        {
            if (input.PausePressed)
            {
                loop.TogglePause();
            }

            if (input.CancelPressed)
            {
                AimingSpell = -1;
            }

            if (input.SpellPressed >= 0)
            {
                SelectSpell(match, input.SpellPressed);
            }

            if (input.SpawnPressed >= 0)
            {
                Spawn(match, input.SpawnPressed);
            }
        }

        public void HandlePointer(RtsMatch match, RtsInput input, RtsCameraRig rig, bool pointerOverUi)
        {
            if (input.LeftPressed == false || pointerOverUi)
            {
                return;
            }

            var point = rig.ScreenToGround(input.Pointer);

            if (AimingSpell >= 0)
            {
                Cast(match, AimingSpell, point);
                return;
            }

            var unit = match.PickUnitAt(point, PickRadius);

            if (unit != null)
            {
                Select(unit);
            }
        }

        // ---- verbs -----------------------------------------------------------------------------

        public void Select(RtsUnit unit)
        {
            Selected = unit;
            InspectingNode = false;
        }

        public void ToggleNodeInspection()
        {
            InspectingNode = InspectingNode == false;
            Selected = null;
        }

        public void SelectSpell(RtsMatch match, int index)
        {
            if (index < 0 || index >= RtsContent.Spells.Count)
            {
                return;
            }

            AimingSpell = AimingSpell == index ? -1 : index;

            if (AimingSpell >= 0 && match.CanCast(_teamIndex, AimingSpell) == false)
            {
                Show(RtsRejectionText.Describe(RtsRejection.OnCooldown, RtsContent.Spells[index].Name));
            }
        }

        public void Cast(RtsMatch match, int index, float2 at)
        {
            if (match.TryCast(_teamIndex, index, at, out var rejection))
            {
                AimingSpell = -1;
                return;
            }

            Show(RtsRejectionText.Describe(rejection, RtsContent.Spells[index].Name));
        }

        public void Spawn(RtsMatch match, int menuIndex)
        {
            var menu = RtsContent.SpawnMenuOf(_teamIndex);

            if (menuIndex < 0 || menuIndex >= menu.Count)
            {
                return;
            }

            if (match.TrySpawn(_teamIndex, menu[menuIndex], out var rejection) == false)
            {
                Show(RtsRejectionText.Describe(rejection, menu[menuIndex].Name));
            }
        }

        public void Research(RtsMatch match, int index)
        {
            if (match.TryResearch(_teamIndex, index, out var rejection) == false)
            {
                Show(RtsRejectionText.Describe(rejection, RtsContent.Researches[index].Name));
            }
        }

        public void LevelUpHero(RtsMatch match)
        {
            if (match.TryLevelUpHero(_teamIndex, out var rejection) == false)
            {
                Show(RtsRejectionText.Describe(rejection));
            }
        }

        // ---- experiments, straight through to the graph -----------------------------------------

        public void Cull(RtsMatch match)
            => Show($"{match.Experiments.Cull(_teamIndex, 5)} of my own units removed");

        public void Prune(RtsMatch match)
            => Show($"pruned {match.Experiments.Prune()} modifier(s)");

        public void Recalculate(RtsMatch match)
            => Show($"recalculated {match.Experiments.RecalculateEverything()} stat(s)");

        public void TryCyclicAura(RtsMatch match)
            => Show(match.Experiments.TryCyclicAura(_teamIndex)
                ? "the cyclic aura was refused, as it should be"
                : "no hero, or the runtime accepted a cycle");

        public void ToggleSloppyDeaths(RtsMatch match)
        {
            var experiments = match.Experiments;

            experiments.SloppyDeaths = experiments.SloppyDeaths == false;

            Show(experiments.SloppyDeaths
                ? "sloppy deaths on — cull some units and watch the observer count"
                : "sloppy deaths off");
        }

        public void ClearSelection()
        {
            Selected = null;
            InspectingNode = false;
            AimingSpell = -1;
        }

        private void Show(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _notice = text;
            _noticeTimer = NoticeSeconds;
        }
    }
}
