using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EncosyTower.SourceGen.Generators.Persistences
{
    using static Helpers;

    partial class PersistenceDeclaration
    {
        public string WriteCode(CancellationToken token)
        {
            var accessorDefs = AccessorDefs;
            var storeDefs = new HashSet<StoreSpec>();

            foreach (var accessorDef in accessorDefs)
            {
                token.ThrowIfCancellationRequested();

                foreach (var arg in accessorDef.Args)
                {
                    token.ThrowIfCancellationRequested();

                    if (arg.StoreDef.IsValid)
                    {
                        storeDefs.Add(arg.StoreDef);
                    }
                }
            }

            token.ThrowIfCancellationRequested();

            var orderedStoreDefs = storeDefs
                .OrderBy(x => x.DataTypeName)
                .ToArray()
                .AsSpan();

            var p = Printer.DefaultLarge;
            p = p.IncreasedIndent();

            var staticKeyword = IsStatic ? "static " : "";

            p.PrintEndLine();
            p.Print("#pragma warning disable").PrintEndLine();
            p.PrintEndLine();

            p.Print("#region    PERSISTENCE").PrintEndLine();
            p.Print("#endregion ===========").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // Persistence");
            p.OpenScope();
            {
                WritePersistence(ref p);
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    READ-ONLY PERSISTENCE").PrintEndLine();
            p.Print("#endregion =====================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // ReadOnlyPersistence");
            p.OpenScope();
            {
                WriteReadOnlyPersistence(ref p, ClassName);
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    ID COLLECTION").PrintEndLine();
            p.Print("#endregion =============").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // StringIdCollection");
            p.OpenScope();
            {
                WriteStringIdCollection(ref p, orderedStoreDefs, token);
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    ACCESSOR COLLECTION").PrintEndLine();
            p.Print("#endregion ===================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // AccessorCollection");
            p.OpenScope();
            {
                WriteAccessorCollection(ref p, accessorDefs, token);
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    READ-ONLY ACCESSOR COLLECTION").PrintEndLine();
            p.Print("#endregion =============================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // ReadOnlyAccessorCollection");
            p.OpenScope();
            {
                WriteReadOnlyAccessorCollection(ref p, accessorDefs, token);
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    ACCESSOR ENUMERATOR").PrintEndLine();
            p.Print("#endregion ===================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // AccessorEnumerator");
            p.OpenScope();
            {
                WriteAccessorEnumerator(ref p);
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    PERSIST DIRECTORY").PrintEndLine();
            p.Print("#endregion =================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // PersistDirectory");
            p.OpenScope();
            {
                WritePersistDirectory(ref p, orderedStoreDefs, token);
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    PERSIST COLLECTION").PrintEndLine();
            p.Print("#endregion ==================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // PersistCollection");
            p.OpenScope();
            {
                WritePersistCollection(ref p, orderedStoreDefs, token);
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    INTERNALS").PrintEndLine();
            p.Print("#endregion =========").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine(staticKeyword).Print("partial class ").Print(ClassName)
                .PrintEndLine(" // Internals");
            p.OpenScope();
            {
                WriteHelpers(ref p);
            }
            p.CloseScope();
            p.PrintEndLine();

            p = p.DecreasedIndent();
            return p.Result;
        }

        private static void WritePersistence(ref Printer p)
        {
            p.PrintLine("/// <summary>");
            p.PrintLine("/// Manages persist stores, accessors, and string ID mappings.");
            p.PrintLine("/// Provides load, save, and lifecycle operations for all persist data associated with an ID.");
            p.PrintLine("/// </summary>");
            p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
            p.PrintLine("internal partial class Persistence : ETP.PersistenceBase");
            p.OpenScope();
            {
                p.PrintLine("internal readonly PersistDirectory _directory;");
                p.PrintLine("internal readonly AccessorCollection _accessors;");
                p.PrintLine("internal readonly StringIdCollection _stringIds;");
                p.PrintLine("internal readonly string _id;");
                p.PrintEndLine();

                p.PrintLine("internal Persistence(");
                p = p.IncreasedIndent();
                {
                    p.PrintBeginLine("  ").Print(NOT_NULL).Print(" ").Print(STRING_VAULT).PrintEndLine(" stringVault");
                    p.PrintBeginLine(", ").Print(NOT_NULL).Print(" ").Print(ENCRYPTION_BASE).PrintEndLine(" encryption");
                    p.PrintBeginLine(", ").Print(NOT_NULL).Print(" ").Print(ILOGGER).PrintEndLine(" logger");
                    p.PrintBeginLine(", ").Print(NOT_NULL).Print(" ").Print(TASK_ARRAY_POOL).PrintEndLine(" taskArrayPool");
                    p.PrintLine(", string id");
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("_stringIds = new(stringVault);");
                    p.PrintLine("_directory = new(stringVault, encryption, logger, taskArrayPool, _stringIds, id);");
                    p.PrintLine("_accessors = new(_directory);");
                    p.PrintLine("_id = id;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public StringIdCollection StringIds");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _stringIds;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public ReadOnlyAccessorCollection Accessors");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _accessors.AsReadOnly();");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public string Id");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _id;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("protected override ETP.IPersistDirectory PersistDirectory");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _directory;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("protected sealed override void OnDeinitialize()");
                p.OpenScope();
                {
                    p.PrintLine("_accessors.Deinitialize();");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("protected sealed override void Dispose(bool disposing)");
                p.OpenScope();
                {
                    p.PrintLine("if (disposing)");
                    p.OpenScope();
                    {
                        p.PrintLine("_directory.Dispose();");
                    }
                    p.CloseScope();
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("protected sealed override UnityTaskBool OnTryLoadAsync(");
                p = p.IncreasedIndent();
                {
                    p.PrintBeginLine("  ").Print(NOT_NULL).Print(" ").Print(ILOGGER).PrintEndLine(" logger");
                    p.PrintLine(", string id");
                    p.PrintLine(", ETP.SourcePriority priority");
                    p.PrintLine(", ETP.SaveDestination destination");
                    p.PrintLine(", ST.CancellationToken token");
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("_accessors.Initialize();");
                    p.PrintLine("return ETT.UnityTasks.GetCompleted(true);");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public ReadOnlyPersistence AsReadOnly()");
                p.OpenScope();
                {
                    p.PrintLine("return new ReadOnlyPersistence(this);");
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteReadOnlyPersistence(ref Printer p, string outerTypeName)
        {
            p.PrintLine("/// <summary>");
            p.PrintLine("/// A read-only view of <see cref=\"Persistence\" /> that exposes safe, non-mutating access");
            p.PrintLine("/// to string IDs, accessors, and save operations.");
            p.PrintLine("/// </summary>");
            p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
            p.PrintLine("public readonly partial struct ReadOnlyPersistence : ET.IIsCreated");
            p.OpenScope();
            {
                p.PrintLine("internal readonly Persistence _persistence;");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("internal ReadOnlyPersistence([SDCA.NotNull] Persistence persistence)");
                p.OpenScope();
                {
                    p.PrintLine("_persistence = persistence;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public bool IsCreated");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _persistence != null;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public StringIdCollection StringIds");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _persistence.StringIds;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public ReadOnlyAccessorCollection Accessors");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _persistence.Accessors;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public string Id");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _persistence.Id;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintBeginLine("public UnityTask SaveAsync")
                    .PrintEndLine("(ETP.SaveDestination destination = default, ST.CancellationToken token = default)");
                p.OpenScope();
                {
                    p.PrintLine("ThrowIfNotCreated(IsCreated);");
                    p.PrintEndLine();

                    p.PrintLine("return _persistence.SaveAsync(destination, token);");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("private static void ThrowIfNotCreated([SDCA.DoesNotReturnIf(false)] bool isCreated)");
                p.OpenScope();
                {
                    p.PrintLine("if (isCreated == false)");
                    p.OpenScope();
                    {
                        p.PrintBeginLine("throw ETDBG.ThrowHelper.CreateInvalidOperationException_TypeNotCreatedCorrectly")
                            .Print("(\"").Print(outerTypeName).PrintEndLine("+ReadOnlyPersistence\");");
                    }
                    p.CloseScope();
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteStringIdCollection(ref Printer p, ReadOnlySpan<StoreSpec> defs, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            p.PrintLine("/// <summary>");
            p.PrintLine("/// An immutable collection of <see cref=\"StringId{T}\" /> values identifying");
            p.PrintLine("/// each data type stored in the persistence layer.");
            p.PrintLine("/// </summary>");
            p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
            p.PrintLine("public readonly partial struct StringIdCollection : ETP.IPersistStringIdCollection, ET.IIsCreated");
            p.OpenScope();
            {
                foreach (var def in defs)
                {
                    token.ThrowIfCancellationRequested();

                    p.PrintBeginLine("public readonly ETS.StringId<string> ").Print(def.DataTypeName)
                        .PrintEndLine(";");
                }

                p.PrintEndLine();

                p.PrintLine("internal StringIdCollection([SDCA.NotNull] ETS.StringVault stringVault)");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        var name = def.DataTypeName;

                        p.PrintBeginLine(name)
                            .Print(" = stringVault.GetOrMakeId(nameof(").Print(name).PrintEndLine("));");
                    }

                    p.PrintLine("IsCreated = true;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public ETS.StringId<string> this[int index] => index switch");
                p.OpenScope();
                {
                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine().Print(i).Print(" => ").Print(defs[i].DataTypeName).PrintEndLine(",");
                    }

                    p.PrintLine("_ => throw ETC.ThrowHelper.CreateIndexOutOfRangeException_Collection()");
                }
                p.CloseScope("};");
                p.PrintEndLine();

                p.PrintLine("public bool IsCreated { get; }");
                p.PrintEndLine();

                p.PrintLine("public int Count");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintBeginLine("get => ").Print(defs.Length).PrintEndLine(";");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public Enumerator GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> new Enumerator(this);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("SCG.IEnumerator<ETS.StringId<string>> SCG.IEnumerable<ETS.StringId<string>>.GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> GetEnumerator();");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("SC.IEnumerator SC.IEnumerable.GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> GetEnumerator();");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(S.Span<ETS.StringId<string>> destination)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(0, destination);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(S.Span<ETS.StringId<string>> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(0, destination, length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(int sourceStartIndex, S.Span<ETS.StringId<string>> destination)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(sourceStartIndex, destination, destination.Length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(int sourceStartIndex, S.Span<ETS.StringId<string>> destination, int length)");
                p.OpenScope();
                {
                    p.PrintLine("var count = Count - sourceStartIndex;");
                    p.PrintEndLine();

                    p.PrintLine("if (length < 0)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETDBG.ThrowHelper.CreateArgumentOutOfRangeException_LengthNegative();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("if (count < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_SourceStartIndex_Length();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("if (destination.Length < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_DestinationTooShort();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("destination = destination[..length];");
                    p.PrintEndLine();

                    p.PrintLine("for (int i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("destination[i] = this[sourceStartIndex + i];");
                    }
                    p.CloseScope();
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(S.Span<ETS.StringId<string>> destination)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(0, destination);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(S.Span<ETS.StringId<string>> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(0, destination, length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(int sourceStartIndex, S.Span<ETS.StringId<string>> destination)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(sourceStartIndex, destination, destination.Length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(int sourceStartIndex, S.Span<ETS.StringId<string>> destination, int length)");
                p.OpenScope();
                {
                    p.PrintLine("var count = Count - sourceStartIndex;");
                    p.PrintEndLine();

                    p.PrintLine("if (length < 0 || count < length || destination.Length < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("return false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("destination = destination[..length];");
                    p.PrintEndLine();

                    p.PrintLine("for (int i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("destination[i] = this[sourceStartIndex + i];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return true;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
                p.PrintLine("public struct Enumerator : SCG.IEnumerator<ETS.StringId<string>>");
                p.OpenScope();
                {
                    p.PrintLine("private readonly StringIdCollection _source;");
                    p.PrintLine("private int _index;");
                    p.PrintEndLine();

                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("public Enumerator(StringIdCollection source)");
                    p.OpenScope();
                    {
                        p.PrintLine("if (source.IsCreated == false)");
                        p.OpenScope();
                        {
                            p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_CollectionNotCreated(\"source\");");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("_source = source;");
                        p.PrintLine("_index = -1;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly ETS.StringId<string> Current => _source[_index];");
                    p.PrintEndLine();

                    p.PrintLine("readonly object SC.IEnumerator.Current => Current;");
                    p.PrintEndLine();

                    p.PrintLine("public void Dispose() { }");
                    p.PrintEndLine();

                    p.PrintLine("public bool MoveNext()");
                    p.OpenScope();
                    {
                        p.PrintLine("_index++;");
                        p.PrintLine("return (uint)_index < (uint)_source.Count;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public void Reset()");
                    p.OpenScope();
                    {
                        p.PrintLine("_index = -1;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteAccessorCollection(
              ref Printer p
            , List<PersistAccessorDeclaration> defs
            , CancellationToken token
        )
        {
            token.ThrowIfCancellationRequested();

            var typeSet = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<PersistAccessorDeclaration>(defs.Count);
            var loopMap = new Dictionary<string, int>(defs.Count, StringComparer.Ordinal);

            foreach (var def in defs)
            {
                token.ThrowIfCancellationRequested();

                queue.Enqueue(def);
            }

            p.PrintLine("/// <summary>");
            p.PrintLine("/// Holds all typed persist accessors and manages their initialization and deinitialization lifecycle.");
            p.PrintLine("/// </summary>");
            p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
            p.PrintLine("internal partial class AccessorCollection : ETP.IPersistAccessorCollection");
            p.OpenScope();
            {
                p.PrintLine("internal AccessorCollection([SDCA.NotNull] PersistDirectory directory)");
                p.OpenScope();
                {
                    var loopBreakCondition = defs.Count;

                    while (queue.Count > 0)
                    {
                        token.ThrowIfCancellationRequested();

                        var def = queue.Dequeue();
                        var needDependency = false;

                        foreach (var arg in def.Args)
                        {
                            token.ThrowIfCancellationRequested();

                            if (string.IsNullOrEmpty(arg.AccessorTypeName) == false && typeSet.Contains(arg.AccessorTypeName) == false)
                            {
                                needDependency = true;
                                break;
                            }
                        }

                        if (needDependency)
                        {
                            if (loopMap.TryGetValue(def.FullTypeName, out var loop))
                            {
                                loop += 1;
                            }

                            if (loop == loopBreakCondition)
                            {
                                break;
                            }

                            loopMap[def.FullTypeName] = loop;
                            queue.Enqueue(def);
                            continue;
                        }

                        typeSet.Add(def.FullTypeName);
                        loopMap.Remove(def.FullTypeName);
                        Write(ref p, def, token);
                    }

                    p.PrintEndLine();

                    foreach (var kv in loopMap)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine("LogErrorCyclicDependency(logger, \"").Print(kv.Key).PrintEndLine("\");");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public ETP.IPersistAccessor this[int index] => index switch");
                p.OpenScope();
                {
                    for (var i = 0; i < defs.Count; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine().Print(i).Print(" => ").Print(defs[i].FieldName).PrintEndLine(",");
                    }

                    p.PrintLine("_ => throw ETC.ThrowHelper.CreateIndexOutOfRangeException_Collection()");
                }
                p.CloseScope("};");
                p.PrintEndLine();

                p.PrintLine("public int Count");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintBeginLine("get => ").Print(defs.Count).PrintEndLine(";");
                }
                p.CloseScope();
                p.PrintEndLine();

                foreach (var def in defs)
                {
                    token.ThrowIfCancellationRequested();

                    var typeName = def.FullTypeName;
                    var fieldName = def.FieldName;

                    p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
                    p.PrintBeginLine("public ").Print(typeName).Print(" ")
                        .Print(fieldName).PrintEndLine(" { get; }")
                        .PrintEndLine();
                }

                p.PrintLine("public void Initialize()");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        if (def.IsInitializable)
                        {
                            p.PrintBeginLine(def.FieldName).PrintEndLine(".Initialize();");
                        }
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void Deinitialize()");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        if (def.IsDeinitializable)
                        {
                            p.PrintBeginLine(def.FieldName).PrintEndLine(".Deinitialize();");
                        }
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public AccessorEnumerator GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> new AccessorEnumerator(new ReadOnlyAccessorCollection(this));");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("SCG.IEnumerator<ETP.IPersistAccessor> SCG.IEnumerable<ETP.IPersistAccessor>.GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> GetEnumerator();");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("SC.IEnumerator SC.IEnumerable.GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> GetEnumerator();");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(S.Span<ETP.IPersistAccessor> destination)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(0, destination);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(S.Span<ETP.IPersistAccessor> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(0, destination, length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(int sourceStartIndex, S.Span<ETP.IPersistAccessor> destination)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(sourceStartIndex, destination, destination.Length);");
                p.PrintEndLine();

                p.PrintLine("public void CopyTo(int sourceStartIndex, S.Span<ETP.IPersistAccessor> destination, int length)");
                p.OpenScope();
                {
                    p.PrintLine("var count = Count - sourceStartIndex;");
                    p.PrintEndLine();

                    p.PrintLine("if (length < 0)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETDBG.ThrowHelper.CreateArgumentOutOfRangeException_LengthNegative();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("if (count < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_SourceStartIndex_Length();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("if (destination.Length < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_DestinationTooShort();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("destination = destination[..length];");
                    p.PrintEndLine();

                    p.PrintLine("for (int i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("destination[i] = this[sourceStartIndex + i];");
                    }
                    p.CloseScope();
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(S.Span<ETP.IPersistAccessor> destination)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(0, destination);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(S.Span<ETP.IPersistAccessor> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(0, destination, length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(int sourceStartIndex, S.Span<ETP.IPersistAccessor> destination)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(sourceStartIndex, destination, destination.Length);");
                p.PrintEndLine();

                p.PrintLine("public bool TryCopyTo(int sourceStartIndex, S.Span<ETP.IPersistAccessor> destination, int length)");
                p.OpenScope();
                {
                    p.PrintLine("var count = Count - sourceStartIndex;");
                    p.PrintEndLine();

                    p.PrintLine("if (length < 0 || count < length || destination.Length < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("return false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("destination = destination[..length];");
                    p.PrintEndLine();

                    p.PrintLine("for (int i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("destination[i] = this[sourceStartIndex + i];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return true;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public ReadOnlyAccessorCollection AsReadOnly()");
                p.OpenScope();
                {
                    p.PrintLine("return new ReadOnlyAccessorCollection(this);");
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();

            return;

            static void Write(ref Printer p, PersistAccessorDeclaration def, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                if (def.Args.Count < 2)
                {
                    p.PrintBeginLine(def.FieldName).Print(" = new(");

                    foreach (var arg in def.Args)
                    {
                        token.ThrowIfCancellationRequested();

                        if (string.IsNullOrEmpty(arg.AccessorTypeName) == false)
                        {
                            p.Print(arg.AccessorTypeName);
                        }
                        else
                        {
                            p.Print("directory.").Print(arg.StoreDef.DataTypeName);
                        }
                    }

                    p.PrintEndLine(");");
                }
                else
                {
                    p.PrintBeginLine(def.SymbolName).PrintEndLine(" = new(");
                    p = p.IncreasedIndent();
                    {
                        var args = def.Args;

                        for (var i = 0; i < args.Count; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            var arg = args[i];
                            var comma = i == 0 ? " " : ",";

                            p.PrintBeginLine(comma).Print(" ");

                            if (string.IsNullOrEmpty(arg.AccessorTypeName) == false)
                            {
                                p.Print(arg.AccessorTypeName);
                            }
                            else
                            {
                                p.Print("directory.").Print(arg.StoreDef.DataTypeName);
                            }

                            p.PrintEndLine();
                        }
                    }
                    p = p.DecreasedIndent();
                    p.PrintLine(");");
                }
            }
        }

        private void WriteReadOnlyAccessorCollection(
              ref Printer p
            , List<PersistAccessorDeclaration> defs
            , CancellationToken token
        )
        {
            token.ThrowIfCancellationRequested();

            p.PrintLine("/// <summary>");
            p.PrintLine("/// A read-only view of <see cref=\"AccessorCollection\" /> that provides immutable,");
            p.PrintLine("/// enumerable access to all typed persist accessors.");
            p.PrintLine("/// </summary>");
            p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
            p.PrintBeginLine("public readonly partial struct ReadOnlyAccessorCollection")
                .PrintEndLine(" : ETP.IPersistAccessorReadOnlyCollection, ET.IIsCreated");
            p.OpenScope();
            {
                p.PrintLine("internal readonly AccessorCollection _accessors;");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("internal ReadOnlyAccessorCollection([SDCA.NotNull] AccessorCollection accessors)");
                p.OpenScope();
                {
                    p.PrintLine("_accessors = accessors;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public bool IsCreated");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _accessors != null;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public ETP.IPersistAccessor this[int index] => index switch");
                p.OpenScope();
                {
                    for (var i = 0; i < defs.Count; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine().Print(i).Print(" => ").Print(defs[i].FieldName).PrintEndLine(",");
                    }

                    p.PrintLine("_ => throw ETC.ThrowHelper.CreateIndexOutOfRangeException_Collection()");
                }
                p.CloseScope("};");
                p.PrintEndLine();

                p.PrintLine("public int Count");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintBeginLine("get => ").Print(defs.Count).PrintEndLine(";");
                }
                p.CloseScope();
                p.PrintEndLine();

                foreach (var def in defs)
                {
                    token.ThrowIfCancellationRequested();

                    var typeName = def.FullTypeName;
                    var fieldName = def.FieldName;

                    p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
                    p.PrintBeginLine("public ").Print(typeName).Print(" ").PrintEndLine(fieldName);
                    p.OpenScope();
                    {
                        p.PrintLine(AGGRESSIVE_INLINING);
                        p.PrintBeginLine("get => _accessors.").Print(fieldName).PrintEndLine(";");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public AccessorEnumerator GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> new AccessorEnumerator(this);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("SCG.IEnumerator<ETP.IPersistAccessor> SCG.IEnumerable<ETP.IPersistAccessor>.GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> GetEnumerator();");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("SC.IEnumerator SC.IEnumerable.GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> GetEnumerator();");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(S.Span<ETP.IPersistAccessor> destination)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(0, destination);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(S.Span<ETP.IPersistAccessor> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(0, destination, length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(int sourceStartIndex, S.Span<ETP.IPersistAccessor> destination)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(sourceStartIndex, destination, destination.Length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(int sourceStartIndex, S.Span<ETP.IPersistAccessor> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> _accessors.CopyTo(sourceStartIndex, destination, length);");

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(S.Span<ETP.IPersistAccessor> destination)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(0, destination);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(S.Span<ETP.IPersistAccessor> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(0, destination, length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(int sourceStartIndex, S.Span<ETP.IPersistAccessor> destination)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(sourceStartIndex, destination, destination.Length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(int sourceStartIndex, S.Span<ETP.IPersistAccessor> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> _accessors.TryCopyTo(sourceStartIndex, destination, length);");
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteAccessorEnumerator(ref Printer p)
        {
            p.PrintLine("/// <summary>");
            p.PrintLine("/// Provides forward-only iteration over the <see cref=\"ETP.IPersistAccessor\" /> elements");
            p.PrintLine("/// held in a <see cref=\"ReadOnlyAccessorCollection\" />.");
            p.PrintLine("/// </summary>");
            p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
            p.PrintLine("public partial struct AccessorEnumerator : SCG.IEnumerator<ETP.IPersistAccessor>");
            p.OpenScope();
            {
                p.PrintLine("private readonly ReadOnlyAccessorCollection _source;");
                p.PrintLine("private int _index;");
                p.PrintEndLine();

                p.PrintLine("internal AccessorEnumerator([SDCA.NotNull] ReadOnlyAccessorCollection source)");
                p.OpenScope();
                {
                    p.PrintLine("if (source.IsCreated == false)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_CollectionNotCreated(\"source\");");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("_source = source;");
                    p.PrintLine("_index = -1;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public readonly ETP.IPersistAccessor Current => _source[_index];");
                p.PrintEndLine();

                p.PrintLine("readonly object SC.IEnumerator.Current => Current;");
                p.PrintEndLine();

                p.PrintLine("public void Dispose() { }");
                p.PrintEndLine();

                p.PrintLine("public bool MoveNext()");
                p.OpenScope();
                {
                    p.PrintLine("_index++;");
                    p.PrintLine("return (uint)_index < (uint)_source.Count;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void Reset()");
                p.OpenScope();
                {
                    p.PrintLine("_index = -1;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WritePersistDirectory(ref Printer p, ReadOnlySpan<StoreSpec> defs, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var generateCreateMethods = new List<StoreSpec>(defs.Length);

            p.PrintLine("/// <summary>");
            p.PrintLine("/// Manages the underlying persist stores for all persist data types, coordinating");
            p.PrintLine("/// load, save, and clone operations across the stores belonging to a single ID.");
            p.PrintLine("/// </summary>");
            p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
            p.PrintLine("internal partial class PersistDirectory : ETP.IPersistDirectory, S.IDisposable");
            p.OpenScope();
            {
                p.PrintBeginLine("private readonly ").Print(STRING_VAULT).PrintEndLine(" _stringVault;");
                p.PrintBeginLine("private readonly ").Print(ENCRYPTION_BASE).PrintEndLine(" _encryption;");
                p.PrintBeginLine("private readonly ").Print(ILOGGER).PrintEndLine(" _logger;");
                p.PrintBeginLine("private readonly ").Print(TASK_ARRAY_POOL).PrintEndLine(" _taskArrayPool;");
                p.PrintLine("private readonly StringIdCollection _stringIds;");
                p.PrintEndLine();

                p.PrintLine("private string _id;");
                p.PrintEndLine();

                p.PrintLine("internal PersistDirectory(");
                p = p.IncreasedIndent();
                {
                    p.PrintBeginLine("  ").Print(NOT_NULL).Print(" ").Print(STRING_VAULT).PrintEndLine(" stringVault");
                    p.PrintBeginLine(", ").Print(NOT_NULL).Print(" ").Print(ENCRYPTION_BASE).PrintEndLine(" encryption");
                    p.PrintBeginLine(", ").Print(NOT_NULL).Print(" ").Print(ILOGGER).PrintEndLine(" logger");
                    p.PrintBeginLine(", ").Print(NOT_NULL).Print(" ").Print(TASK_ARRAY_POOL).PrintEndLine(" taskArrayPool");
                    p.PrintLine(", StringIdCollection stringIds");
                    p.PrintLine(", string id");
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("_stringVault = stringVault;");
                    p.PrintLine("_encryption = encryption;");
                    p.PrintLine("_logger = logger;");
                    p.PrintLine("_taskArrayPool = taskArrayPool;");
                    p.PrintLine("_stringIds = stringIds;");
                    p.PrintLine("_id = id;");
                    p.PrintEndLine();

                    p.PrintLine("bool ignoreEncryption = false;");
                    p.PrintEndLine();

                    p.Print("#if !FORCE_PERSIST_ENCRYPTION").PrintEndLine();
                    p.PrintLine("GetIgnoreEncryption(ref ignoreEncryption);");
                    p.Print("#endif").PrintEndLine();
                    p.PrintEndLine();

                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        var hasDefaultConstructor = def.DataTypeHasDefaultConstructor;

                        if (hasDefaultConstructor == false)
                        {
                            generateCreateMethods.Add(def);
                        }

                        p.OpenScope();
                        {
                            p.PrintBeginLine("S.Func<").Print(def.FullDataTypeName).Print("> ")
                                .Print("createFunc = ");

                            if (hasDefaultConstructor)
                            {
                                p.Print("static () => new ").Print(def.FullDataTypeName).PrintEndLine("();");
                            }
                            else
                            {
                                p.Print("Create").Print(def.DataTypeName).PrintEndLine(";");
                            }

                            p.PrintBeginLine("var args = GetStoreArgs<")
                                .Print(def.FullDataTypeName).Print(", ").Print(def.FullStoreTypeName)
                                .PrintEndLine(">(createFunc);");

                            p.PrintBeginLine(def.DataTypeName).Print(" = new(")
                                .Print("_stringIds.").Print(def.DataTypeName)
                                .Print(", stringVault, encryption, logger, ignoreEncryption, args")
                                .PrintEndLine(") { Id = id };");
                        }
                        p.CloseScope();
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                foreach (var def in defs)
                {
                    token.ThrowIfCancellationRequested();

                    p.PrintBeginLine("public ")
                        .Print(def.FullStoreTypeName).Print(" ")
                        .Print(def.DataTypeName).PrintEndLine(" { get; }")
                        .PrintEndLine();
                }

                p.PrintLine("public string Id");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get");
                    p.OpenScope();
                    {
                        p.PrintLine("return _id;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("set");
                    p.OpenScope();
                    {
                        p.PrintLine("_id = value;");
                        p.PrintEndLine();

                        foreach (var def in defs)
                        {
                            token.ThrowIfCancellationRequested();

                            p.PrintBeginLine(def.DataTypeName)
                                .PrintEndLine(".Id = value;");
                        }
                    }
                    p.CloseScope();
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("static partial void GetIgnoreEncryption(ref bool ignoreEncryption);");
                p.PrintEndLine();

                p.PrintBeginLine("private static partial ").Print(STORE_ARGS)
                    .PrintEndLine(" GetStoreArgs<TData, TStore>(S.Func<TData> createDataFunc)");
                p.WithIncreasedIndent().PrintLine("where TData : ETP.IPersist");
                p.WithIncreasedIndent().PrintBeginLine("where TStore : ").Print(STORE_BASE).PrintEndLine("TData>;");
                p.PrintEndLine();

                foreach (var def in generateCreateMethods)
                {
                    token.ThrowIfCancellationRequested();

                    p.PrintBeginLine("private static partial ").Print(def.FullDataTypeName)
                        .Print(" Create").Print(def.DataTypeName).PrintEndLine("();");
                    p.PrintEndLine();
                }

                p.PrintLine("public void Initialize()");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine(def.DataTypeName)
                            .PrintEndLine(".Initialize();");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void Deinitialize()");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine(def.DataTypeName)
                            .PrintEndLine(".Deinitialize();");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void Dispose()");
                p.OpenScope();
                {
                    p.PrintLine("_encryption.Dispose();");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void CreateDataIfNotExist()");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine("if (").Print(def.DataTypeName).PrintEndLine(".IsDataValid == false)");
                        p.OpenScope();
                        {
                            p.PrintBeginLine(def.DataTypeName).PrintEndLine(".CreateData();");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void CreateData()");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine(def.DataTypeName).PrintEndLine(".CreateData();");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void MarkDirty(bool isDirty)");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine(def.DataTypeName).PrintEndLine(".MarkDirty(isDirty);");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void SetIdAndVersion(");
                p = p.IncreasedIndent();
                {
                    p.PrintLine("  string id");
                    p.PrintLine(", int version");

                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine(", bool include").Print(defs[i].DataTypeName).PrintEndLine(" = true");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine("if (include").Print(def.DataTypeName).PrintEndLine(")");
                        p.OpenScope();
                        {
                            p.PrintBeginLine(def.DataTypeName).PrintEndLine(".SetIdAndVersion(id, version);");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public UnityTask LoadEntireDirectoryAsync(ETP.SourcePriority priority = default, ST.CancellationToken token = default)");
                p.WithIncreasedIndent().PrintLine("=> LoadAsync(priority, token);");
                p.PrintEndLine();

                p.PrintBeginLine("public ").PrintIf(defs.Length > 0, "async ")
                    .PrintEndLine("UnityTask LoadAsync(");
                p = p.IncreasedIndent();
                {
                    p.PrintLine("  ETP.SourcePriority priority");
                    p.PrintLine(", ST.CancellationToken token = default");

                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine(", bool include").Print(defs[i].DataTypeName).PrintEndLine(" = true");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    if (defs.Length > 1)
                    {
                        p.PrintBeginLine("var tasks = _taskArrayPool.Rent(").Print(defs.Length).PrintEndLine(");");
                        p.PrintEndLine();

                        var lastIndex = defs.Length - 1;

                        for (var i = 0; i < defs.Length; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            var name = defs[i].DataTypeName;

                            p.PrintBeginLine("tasks[").Print(i).Print("] = include").PrintEndLine(name);
                            p = p.IncreasedIndent();
                            {
                                p.PrintBeginLine("? ").Print(name).PrintEndLine(".LoadAsync(priority, token)");
                                p.PrintBeginLine(": ").Print(COMPLETED_TASK).PrintEndLine(";");
                            }
                            p = p.DecreasedIndent();

                            p.PrintEndLineIf(i < lastIndex, "");
                        }

                        p.PrintEndLine();
                        p.PrintBeginLine("await ").Print(WHEN_ALL_TASKS).PrintEndLine(";");

                        p.PrintEndLine();
                        p.PrintLine("_taskArrayPool.Return(tasks, true);");
                    }
                    else if (defs.Length == 1)
                    {
                        var name = defs[0].DataTypeName;

                        p.PrintBeginLine("if (include").Print(name).PrintEndLine(")");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("await ").Print(name).PrintEndLine(".LoadAsync(priority, token);");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                    else
                    {
                        p.PrintBeginLine("return ").Print(COMPLETED_TASK).PrintEndLine(";");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public UnityTask SaveEntireDirectoryAsync(ETP.SaveDestination destination = default, ST.CancellationToken token = default)");
                p.WithIncreasedIndent().PrintLine("=> SaveAsync(destination, token);");
                p.PrintEndLine();

                p.PrintBeginLine("public ").PrintIf(defs.Length > 0, "async ")
                    .PrintEndLine("UnityTask SaveAsync(");
                p = p.IncreasedIndent();
                {
                    p.PrintLine("  ETP.SaveDestination destination = default");
                    p.PrintLine(", ST.CancellationToken token = default");

                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine(", bool include").Print(defs[i].DataTypeName).PrintEndLine(" = true");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    if (defs.Length > 1)
                    {
                        p.PrintBeginLine("var tasks = _taskArrayPool.Rent(").Print(defs.Length).PrintEndLine(");");
                        p.PrintEndLine();

                        var lastIndex = defs.Length - 1;

                        for (var i = 0; i < defs.Length; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            var name = defs[i].DataTypeName;

                            p.PrintBeginLine("tasks[").Print(i).Print("] = include").PrintEndLine(name);
                            p = p.IncreasedIndent();
                            {
                                p.PrintBeginLine("? ").Print(name).PrintEndLine(".SaveAsync(destination, token)");
                                p.PrintBeginLine(": ").Print(COMPLETED_TASK).PrintEndLine(";");
                            }
                            p = p.DecreasedIndent();

                            p.PrintEndLineIf(i < lastIndex, "");
                        }

                        p.PrintEndLine();
                        p.PrintBeginLine("await ").Print(WHEN_ALL_TASKS).PrintEndLine(";");

                        p.PrintEndLine();
                        p.PrintLine("_taskArrayPool.Return(tasks, true);");
                    }
                    else if (defs.Length == 1)
                    {
                        var name = defs[0].DataTypeName;

                        p.PrintBeginLine("if (include").Print(name).PrintEndLine(")");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("await ").Print(name).PrintEndLine(".SaveAsync(destination, token);");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                    else
                    {
                        p.PrintBeginLine("return ").Print(COMPLETED_TASK).PrintEndLine(";");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public void CloneDataFromRemote(");
                p = p.IncreasedIndent();
                {
                    p.PrintLine("  ETP.SourcePriority priority = default");

                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine(", bool include").Print(defs[i].DataTypeName).PrintEndLine(" = true");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine("if (include").Print(def.DataTypeName).PrintEndLine(")");
                        p.OpenScope();
                        {
                            p.PrintBeginLine(def.DataTypeName).PrintEndLine(".TryCloneDataFromRemote();");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WritePersistCollection(ref Printer p, ReadOnlySpan<StoreSpec> defs, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            p.PrintLine("/// <summary>");
            p.PrintLine("/// A serializable, value-type snapshot of all persist data instances belonging to a single ID.");
            p.PrintLine("/// Supports copying to and from the <see cref=\"PersistDirectory\" />.");
            p.PrintLine("/// </summary>");
            p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
            p.PrintLine("[S.Serializable]");
            p.PrintLine("public partial struct PersistCollection : ETP.IPersistCollection, ET.IIsCreated");
            p.OpenScope();
            {
                p.PrintLine("[UE.SerializeField] internal string _id;");

                foreach (var def in defs)
                {
                    token.ThrowIfCancellationRequested();

                    p.PrintBeginLine("[UE.SerializeField] internal ").Print(def.FullDataTypeName).Print(" ")
                        .Print(def.DataFieldName).PrintEndLine(";");
                }

                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public PersistCollection(");
                p = p.IncreasedIndent();
                {
                    p.PrintLine("  [SDCA.NotNull] string id");
                    p.PrintLine(", PersistCollection source");
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("if (source.IsCreated == false)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_CollectionNotCreated(\"source\");");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("_id = id;");

                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        var def = defs[i];

                        p.PrintBeginLine(def.DataFieldName).Print(" = source.").Print(def.DataFieldName).PrintEndLine(";");
                    }

                    p.PrintEndLine();
                    p.PrintLine("IsCreated = true;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("internal PersistCollection(");
                p = p.IncreasedIndent();
                {
                    p.PrintLine("  [SDCA.NotNull] string id");

                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        var def = defs[i];

                        p.PrintBeginLine(", ").Print(def.FullDataTypeName).Print(" ").PrintEndLine(def.DataArgName);
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("_id = id;");

                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        var def = defs[i];

                        p.PrintBeginLine(def.DataFieldName).Print(" = ").Print(def.DataArgName).PrintEndLine(";");
                    }

                    p.PrintEndLine();
                    p.PrintLine("IsCreated = true;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public ETP.IPersist this[int index] => index switch");
                p.OpenScope();
                {
                    for (var i = 0; i < defs.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        p.PrintBeginLine().Print(i).Print(" => ").Print(defs[i].DataFieldName).PrintEndLine(",");
                    }

                    p.PrintLine("_ => throw ETC.ThrowHelper.CreateIndexOutOfRangeException_Collection()");

                }
                p.CloseScope("};");
                p.PrintEndLine();

                p.PrintLine("public bool IsCreated { get; }");
                p.PrintEndLine();

                p.PrintLine("public int Count");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintBeginLine("get => ").Print(defs.Length).PrintEndLine(";");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public string Id");
                p.OpenScope();
                {
                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("get => _id;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintBeginLine("public static PersistCollection GetFrom(ReadOnlyPersistence persistence, ")
                    .PrintEndLine("ETP.SourcePriority priority = default)");
                p.OpenScope();
                {
                    p.PrintLine("if (persistence.IsCreated == false)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETDBG.ThrowHelper.CreateArgumentNullException(\"persistence\");");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("var directory = persistence._persistence._directory;");
                    p.PrintEndLine();

                    p.PrintLine("return new PersistCollection(");
                    p = p.IncreasedIndent();
                    {
                        p.PrintLine("  directory.Id");

                        foreach (var def in defs)
                        {
                            token.ThrowIfCancellationRequested();

                            var name = def.DataTypeName;

                            p.PrintBeginLine(", directory.").Print(name).PrintEndLine(".GetData(priority)");
                        }
                    }
                    p = p.DecreasedIndent();
                    p.PrintLine(");");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintBeginLine("public static PersistCollection CloneFrom(ReadOnlyPersistence persistence, ")
                    .PrintEndLine("ETP.SourcePriority priority = default)");
                p.OpenScope();
                {
                    p.PrintLine("if (persistence.IsCreated == false)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETDBG.ThrowHelper.CreateArgumentNullException(\"persistence\");");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("var directory = persistence._persistence._directory;");
                    p.PrintEndLine();

                    p.PrintLine("return new PersistCollection(");
                    p = p.IncreasedIndent();
                    {
                        p.PrintLine("  directory.Id");

                        foreach (var def in defs)
                        {
                            token.ThrowIfCancellationRequested();

                            var name = def.DataTypeName;
                            p.PrintBeginLine(", directory.").Print(name)
                                .PrintEndLine(".TryCloneData(priority).GetValueOrDefault()");
                        }
                    }
                    p = p.DecreasedIndent();
                    p.PrintLine(");");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public PersistCollection ChangeId([SDCA.NotNull] string id)");
                p.WithIncreasedIndent().PrintLine("=> new PersistCollection(id, this);");
                p.PrintEndLine();

                p.PrintLine("public void SetTo(ReadOnlyPersistence persistence)");
                p.OpenScope();
                {
                    p.PrintLine("if (IsCreated == false)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateInvalidOperationException_CollectionNotCreated();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("if (persistence.IsCreated == false)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETDBG.ThrowHelper.CreateArgumentNullException(\"persistence\");");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("var directory = persistence._persistence._directory;");
                    p.PrintEndLine();

                    foreach (var def in defs)
                    {
                        token.ThrowIfCancellationRequested();

                        var name = def.DataTypeName;
                        var fieldName = def.DataFieldName;

                        p.PrintBeginLine("directory.").Print(name)
                            .Print(".SetData(").Print(fieldName).PrintEndLine(");");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public Enumerator GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> new Enumerator(this);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("SCG.IEnumerator<ETP.IPersist> SCG.IEnumerable<ETP.IPersist>.GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> GetEnumerator();");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("SC.IEnumerator SC.IEnumerable.GetEnumerator()");
                p.WithIncreasedIndent().PrintLine("=> GetEnumerator();");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(S.Span<ETP.IPersist> destination)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(0, destination);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(S.Span<ETP.IPersist> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(0, destination, length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(int sourceStartIndex, S.Span<ETP.IPersist> destination)");
                p.WithIncreasedIndent().PrintLine("=> CopyTo(sourceStartIndex, destination, destination.Length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public void CopyTo(int sourceStartIndex, S.Span<ETP.IPersist> destination, int length)");
                p.OpenScope();
                {
                    p.PrintLine("var count = Count - sourceStartIndex;");
                    p.PrintEndLine();

                    p.PrintLine("if (length < 0)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETDBG.ThrowHelper.CreateArgumentOutOfRangeException_LengthNegative();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("if (count < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_SourceStartIndex_Length();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("if (destination.Length < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_DestinationTooShort();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("destination = destination[..length];");
                    p.PrintEndLine();

                    p.PrintLine("for (int i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("destination[i] = this[sourceStartIndex + i];");
                    }
                    p.CloseScope();
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(S.Span<ETP.IPersist> destination)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(0, destination);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(S.Span<ETP.IPersist> destination, int length)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(0, destination, length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(int sourceStartIndex, S.Span<ETP.IPersist> destination)");
                p.WithIncreasedIndent().PrintLine("=> TryCopyTo(sourceStartIndex, destination, destination.Length);");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintLine("public bool TryCopyTo(int sourceStartIndex, S.Span<ETP.IPersist> destination, int length)");
                p.OpenScope();
                {
                    p.PrintLine("var count = Count - sourceStartIndex;");
                    p.PrintEndLine();

                    p.PrintLine("if (length < 0 || count < length || destination.Length < length)");
                    p.OpenScope();
                    {
                        p.PrintLine("return false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("destination = destination[..length];");
                    p.PrintEndLine();

                    p.PrintLine("for (int i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("destination[i] = this[sourceStartIndex + i];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return true;");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintBeginLine(EXCLUDE_COVERAGE).PrintEndLine(GENERATED_CODE);
                p.PrintLine("public struct Enumerator : SCG.IEnumerator<ETP.IPersist>");
                p.OpenScope();
                {
                    p.PrintLine("private readonly PersistCollection _source;");
                    p.PrintLine("private int _index;");
                    p.PrintEndLine();

                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintLine("public Enumerator([SDCA.NotNull] PersistCollection source)");
                    p.OpenScope();
                    {
                        p.PrintLine("if (source.IsCreated == false)");
                        p.OpenScope();
                        {
                            p.PrintLine("throw ETC.ThrowHelper.CreateArgumentException_CollectionNotCreated(\"source\");");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("_source = source;");
                        p.PrintLine("_index = -1;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly ETP.IPersist Current => _source[_index];");
                    p.PrintEndLine();

                    p.PrintLine("readonly object SC.IEnumerator.Current => Current;");
                    p.PrintEndLine();

                    p.PrintLine("public void Dispose() { }");
                    p.PrintEndLine();

                    p.PrintLine("public bool MoveNext()");
                    p.OpenScope();
                    {
                        p.PrintLine("_index++;");
                        p.PrintLine("return (uint)_index < (uint)_source.Count;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public void Reset()");
                    p.OpenScope();
                    {
                        p.PrintLine("_index = -1;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
                p.CloseScope();
            }
            p.CloseScope();
        }

        private static void WriteHelpers(ref Printer p)
        {
            p.PrintBeginLine("private const string GENERATOR = ").Print(GENERATOR).PrintEndLine(";");
            p.PrintEndLine();

            p.PrintLine(HIDE_IN_CALL_STACK);
            p.PrintLine("private static void LogErrorCyclicDependency(ETL.ILogger logger, string name)");
            p.OpenScope();
            {
                p.PrintBeginLine("logger.LogError(")
                    .PrintEndLine("$\"Detect cyclic dependency in the constructor of type '{name}'\");");
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }
    }
}
