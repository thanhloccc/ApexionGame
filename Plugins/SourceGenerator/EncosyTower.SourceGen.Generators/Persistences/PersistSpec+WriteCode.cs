namespace EncosyTower.SourceGen.Generators.Persistences
{
    internal partial struct PersistSpec
    {
        private const string AGGRESSIVE_INLINING = "[SRCS.MethodImpl(SRCS.MethodImplOptions.AggressiveInlining)]";
        private const string EXCLUDE_COVERAGE = "[SDCA.ExcludeFromCodeCoverage]";
        private const string GENERATED_CODE = $"[SCDC.GeneratedCode(\"EncosyTower.SourceGen.Generators.Persistences.PersistGenerator\", \"{SourceGenVersion.VALUE}\")]";

        public readonly string WriteCode()
        {
            var p = Printer.DefaultLarge;

            p.PrintEndLine();
            p.Print("#pragma warning disable").PrintEndLine();
            p.PrintEndLine();

            p = p.IncreasedIndent();
            {
                p.PrintBeginLine(GENERATED_CODE).PrintEndLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine("partial ").Print(typeKeyword).Print(" ").Print(typeName);

                if (generateInterface)
                {
                    p.Print(" : ETUV.IPersist");
                }

                p.PrintEndLine();
                p.OpenScope();
                {
                    var generatesId = memberId.ShouldGenerate;
                    var generatesVersion = memberVersion.ShouldGenerate;

                    if (generatesId)
                    {
                        WriteProperty(ref p, "Id", "string", memberId);

                        if (generatesVersion)
                        {
                            p.PrintEndLine();
                        }
                    }

                    if (generatesVersion)
                    {
                        WriteProperty(ref p, "Version", "int", memberVersion);
                    }
                }
                p.CloseScope();
            }
            p = p.DecreasedIndent();

            return p.Result;
        }

        private static void WriteProperty(ref Printer p, string name, string type, MemberDefinition member)
        {
            var modifier = member.type == MemberDefinitionType.DefinedInBaseTypeAsAbstract
                ? "public override "
                : "public ";

            if (member.isField && member.forwardedAttributes.Count > 0)
            {
                foreach (var attr in member.forwardedAttributes)
                {
                    p.PrintBeginLine("[").Print(attr.syntax).PrintEndLine("]");
                }
            }

            p.PrintBeginLine(modifier).Print(type).Print(" ").Print(name);

            if (member.isField == false)
            {
                p.PrintEndLine(" { get; set; }");
                return;
            }

            p.PrintEndLine();
            p.OpenScope();
            {
                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintBeginLine("get => ")
                    .PrintIf(member.type == MemberDefinitionType.DefinedInBaseType, "base", "this")
                    .Print(".").Print(member.name).PrintEndLine(";");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintBeginLine("set => ")
                    .PrintIf(member.type == MemberDefinitionType.DefinedInBaseType, "base", "this")
                    .Print(".").Print(member.name).PrintEndLine(" = value;");
            }
            p.CloseScope();
        }
    }
}
