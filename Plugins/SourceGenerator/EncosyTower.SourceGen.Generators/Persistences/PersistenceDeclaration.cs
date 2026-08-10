using System.Collections.Generic;

namespace EncosyTower.SourceGen.Generators.Persistences
{
    internal partial class PersistenceDeclaration
    {
        public string ClassName { get; }

        public bool IsStatic { get; }

        public List<PersistAccessorDeclaration> AccessorDefs { get; }

        public PersistenceDeclaration(
              string className
            , bool isStatic
            , List<PersistAccessorDeclaration> accessorDefs
        )
        {
            ClassName = className;
            IsStatic = isStatic;
            AccessorDefs = accessorDefs;
        }
    }
}
