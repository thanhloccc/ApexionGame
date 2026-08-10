using System;
using ApexionGame.HFSM.Samples;
using UnityEditor;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Samples.Editor;

/// <summary>
/// The ten inspector buttons from HFSM - Layout.md §5, step 8.2. Each button is a plain
/// <see cref="Action"/> closing over its scenario number — never <c>Invoke(name)</c> — so pressing
/// any button in any order rebuilds a clean machine and re-runs exactly one scenario.
/// </summary>
[CustomEditor(typeof(MachinePlayground))]
public sealed class MachinePlaygroundEditor : UnityEditor.Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var output = new Label { style = { whiteSpace = WhiteSpace.Normal } };

        for (var scenario = 1; scenario <= MachinePlaygroundWorld.ScenarioCount; scenario++)
        {
            var captured = scenario;
            Action onClick = () => Run(captured, output);
            root.Add(new Button(onClick) { text = MachinePlaygroundWorld.TitleOf(captured) });
        }

        root.Add(output);
        Refresh(output);
        return root;
    }

    private void Run(int scenario, Label output)
    {
        ((MachinePlayground)target).RunScenario(scenario);
        Refresh(output);
    }

    private void Refresh(Label output)
    {
        output.text = ((MachinePlayground)target).LastOutput;
    }
}
