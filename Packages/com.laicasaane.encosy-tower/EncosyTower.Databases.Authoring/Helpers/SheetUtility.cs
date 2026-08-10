using System;
using System.IO;
using System.Text;
using EncosyTower.Pooling;

namespace EncosyTower.Databases.Authoring
{
    public static class SheetUtility
    {
        public static bool ValidateSheetName(string name, bool allowComments = false, bool allowWhiteSpaces = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            name = name.Trim();

            if (allowWhiteSpaces == false && name.Contains(' ', StringComparison.Ordinal))
            {
                return false;
            }

            if (allowComments)
            {
                return true;
            }

            return name.StartsWith('$') == false
                && name.EndsWith('$') == false
                && name.StartsWith('<') == false
                && name.EndsWith('>') == false
                ;
        }

        public static string ToFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            using var _ = StringBuilderPool.Rent(out var sb);

            sb = sb.Append(name);
            sb = Trim(sb);
            sb = ReplaceStartEndAngleBrackets(sb, out var replacedStart, out var replacedEnd);

            var start = replacedStart ? 0 : 1;
            var end = replacedEnd ? sb.Length - 2 : sb.Length - 1;
            sb = RemoveInvalidFileNameChars(sb, start, end);

            return sb.ToString();
        }

        public static string ToFileName(string name, int index)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return $"${index}";
            }

            using var _ = StringBuilderPool.Rent(out var sb);

            sb = sb.Append(name);
            sb = Trim(sb);
            sb = ReplaceStartEndAngleBrackets(sb, out var replacedStart, out var replacedEnd);

            var start = replacedStart ? 1 : 0;
            var end = replacedEnd ? sb.Length - 2 : sb.Length - 1;
            sb = RemoveInvalidFileNameChars(sb, start, end);

            return sb.ToString();
        }

        private static StringBuilder ReplaceStartEndAngleBrackets(
              StringBuilder sb
            , out bool replacedStart
            , out bool replacedEnd
        )
        {
            replacedStart = false;
            replacedEnd = false;

            if (sb.Length > 0)
            {
                if (sb[^1] == '>')
                {
                    sb.Remove(sb.Length - 1, 1).Append('$');
                    replacedEnd = true;
                }

                if (sb[0] == '<')
                {
                    sb.Remove(0, 1).Insert(0, '$');
                    replacedStart = true;
                }
            }

            return sb;
        }

        private static StringBuilder RemoveInvalidFileNameChars(StringBuilder sb, int start, int end)
        {
            var count = end - start + 1;
            var invalidChars = Path.GetInvalidFileNameChars();

            foreach (var c in invalidChars)
            {
                sb.Replace(c, '_', start, count);
            }

            return sb;
        }

        private static StringBuilder Trim(StringBuilder sb)
        {
            if (sb.Length < 1)
            {
                return sb;
            }

            int start = 0;
            int end = sb.Length - 1;

            while (end >= start && char.IsWhiteSpace(sb[end]))
            {
                end--;
            }

            if (end < sb.Length - 1)
            {
                sb.Remove(end + 1, sb.Length - end - 1);
            }

            end = sb.Length - 1;

            while (start <= end && char.IsWhiteSpace(sb[start]))
            {
                start++;
            }

            if (start > 0)
            {
                sb.Remove(0, start);
            }

            return sb;
        }
    }
}
