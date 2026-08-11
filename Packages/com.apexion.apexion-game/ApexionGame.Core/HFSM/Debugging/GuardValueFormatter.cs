using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// Best-effort <c>"Field = value"</c> read for a guard's <c>[CallerArgumentExpression]</c>
    /// source text, for the guard inspector's value column.
    /// </summary>
    /// <remarks>
    /// Matches the single simple shape documented in HFSM - Debugging.md §4.2: a plain read of one
    /// blackboard field or property, <c>&lt;param&gt;.&lt;Field&gt; &lt;op&gt; &lt;literal&gt;</c>.
    /// Anything else — a method call, a boolean combination, a local variable — does not match, and
    /// the caller falls back to showing the expression and the boolean result alone. This is a
    /// deliberate degradation, not a bug: a heuristic that tried to cover every guard shape would be
    /// a small expression evaluator, which is far more than a debug label needs.
    /// </remarks>
    public static class GuardValueFormatter
    {
        private static readonly Regex s_fieldRead = new(
              @"^\s*\w+\.(\w+)\s*(?:<=|>=|==|!=|<|>)"
            , RegexOptions.Compiled
        );

        // Reflection lookups are once-per-(type, field) and only ever run from the debug window's
        // poll, never from the tick path — a plain Dictionary is fine, there is no concurrent writer.
        private static readonly Dictionary<(Type, string), MemberInfo> s_members = new();

        /// <summary>
        /// Reads the field or property that <paramref name="expression"/> compares, off
        /// <paramref name="context"/>, formatted as <c>"Field = value"</c>.
        /// </summary>
        public static bool TryFormat(object context, string expression, out string text)
        {
            text = null;

            if (context == null || string.IsNullOrEmpty(expression))
            {
                return false;
            }

            var match = s_fieldRead.Match(expression);

            if (match.Success == false)
            {
                return false;
            }

            var fieldName = match.Groups[1].Value;
            var member = ResolveMember(context.GetType(), fieldName);

            if (member == null)
            {
                return false;
            }

            var value = member is FieldInfo field
                ? field.GetValue(context)
                : ((PropertyInfo)member).GetValue(context);

            text = $"{fieldName} = {value}";
            return true;
        }

        private static MemberInfo ResolveMember(Type type, string fieldName)
        {
            var key = (type, fieldName);

            if (s_members.TryGetValue(key, out var member))
            {
                return member;
            }

            const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.Instance;

            member = (MemberInfo)type.GetField(fieldName, FLAGS)
                ?? type.GetProperty(fieldName, FLAGS);

            // A failed lookup is cached too — a guard whose field is private or misspelled would
            // otherwise repeat the same two failed reflection calls on every poll of every machine.
            s_members[key] = member;
            return member;
        }
    }
}
