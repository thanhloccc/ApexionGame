using UnityEngine;

namespace ApexionGame.HFSM.Samples;

/// <summary>
/// Hosts the ten scenarios in <see cref="MachinePlaygroundWorld"/> so they can be run from the
/// scene or, through <c>MachinePlaygroundEditor</c>, from the inspector — in or out of Play mode.
/// </summary>
[ExecuteAlways]
public sealed class MachinePlayground : MonoBehaviour
{
    [SerializeField, TextArea(4, 20)]
    private string _lastOutput = "Press a button to run a scenario.";

    private MachinePlaygroundWorld _world;

    public string LastOutput => _lastOutput;

    public void RunScenario(int scenario)
    {
        _world ??= new MachinePlaygroundWorld();
        _lastOutput = $"{MachinePlaygroundWorld.TitleOf(scenario)}\n\n{_world.Run(scenario)}";
    }

    private void OnDisable()
    {
        _world?.Dispose();
        _world = null;
    }
}
