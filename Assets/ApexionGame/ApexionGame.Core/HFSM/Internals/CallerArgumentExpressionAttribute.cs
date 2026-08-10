#if !NET5_0_OR_GREATER

// Deliberately in System.Runtime.CompilerServices, not ApexionGame.HFSM.Internals: the C# compiler
// resolves [CallerArgumentExpression] by this exact namespace-qualified name. Unity's netstandard
//2.1 API surface ships its own copy of this attribute marked internal to its own assembly, which
// satisfies the *compiler feature* (recognizing the attribute on a parameter) but leaves user code
// unable to reference the type by name — hence "inaccessible due to its protection level" rather
// than "type not found". A type declared in the compiling assembly takes precedence over one from a
// reference, and `internal` is sufficient here: only this assembly's own compiler pass needs to see
// it, since MachineBuilder`2.cs is the only place that declares an attributed parameter, and the
// value it captures is filled in as plain metadata at each call site regardless of which assembly is
// calling from.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}

#endif
