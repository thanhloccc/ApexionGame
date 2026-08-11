using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Editor.Views
{
    /// <summary>
    /// One node box in <see cref="StateGraphView"/>. Split out because the graph is a pool of these
    /// rather than a fresh tree per poll — matching the <c>Type.cs</c> one-element-per-file rule.
    /// </summary>
    public sealed class StateNodeElement : VisualElement
    {
        public static readonly string UssClassName = "hfsm-graph__node";
        public static readonly string NameUssClassName = $"{UssClassName}-name";
        public static readonly string KindUssClassName = $"{UssClassName}-kind";
        public static readonly string ActiveUssClassName = $"{UssClassName}--active";
        public static readonly string ActiveLeafUssClassName = $"{UssClassName}--active-leaf";
        public static readonly string ParallelUssClassName = $"{UssClassName}--parallel";
        public static readonly string SelectedUssClassName = $"{UssClassName}--selected";
        public static readonly string DimmedUssClassName = $"{UssClassName}--dimmed";

        public readonly Label Name;
        public readonly Label Kind;

        public NodeIndex Node { get; private set; }

        public StateNodeElement()
        {
            AddToClassList(UssClassName);

            Name = new Label();
            Name.AddToClassList(NameUssClassName);
            Add(Name);

            Kind = new Label();
            Kind.AddToClassList(KindUssClassName);
            Add(Kind);
        }

        public void Bind(NodeIndex node, string name, string kindLabel)
        {
            Node = node;
            Name.text = name;
            Kind.text = kindLabel;
        }
    }
}
