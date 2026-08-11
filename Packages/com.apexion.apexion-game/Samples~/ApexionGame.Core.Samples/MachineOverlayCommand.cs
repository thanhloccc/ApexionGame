#if (UNITY_EDITOR || DEVELOPMENT_BUILD || APEXION_HFSM_DEBUG) && !DISABLE_APEXION_CHECKS

using ApexionGame.HFSM.Debugging;
using EncosyTower.Annotations;
using EncosyTower.Mvvm.ComponentModel;
using EncosyTower.Mvvm.Input;
using EncosyTower.VisualDebugging.Commands;
using UnityEngine;

namespace ApexionGame.HFSM.Samples;

/// <summary>
/// Toggles <see cref="MachineOverlayBehaviour"/> from the in-game console (HFSM - Layout.md §5,
/// step 8.4). Lives here rather than on <c>ApexionGame.Core</c> because the console needs
/// <c>EncosyTower.Core.Extended</c> — a consumer that wants only the state machine must not pay for
/// it (DEC-011).
/// </summary>
[ObservableObject]
[Label("Toggle Overlay", "HFSM")]
internal sealed partial class MachineOverlayCommand : IVisualCommand
{
    [ObservableProperty]
    private bool Visible
    {
        get => Get_Visible();
        set => Set_Visible(value);
    }

    public MachineOverlayCommand()
    {
        _visible = true;
    }

    public void Execute()
    {
        Visible = !Visible;

        var overlay = Object.FindFirstObjectByType<MachineOverlayBehaviour>(FindObjectsInactive.Include);
        overlay?.SetVisible(Visible);
    }

    [RelayCommand]
    private void SetVisible(bool value)
    {
        _visible = value;
    }
}

#endif
