using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Inedo.BuildMasterExtensions.LeanKit
{
    internal static class JsonReader
    {
        private static readonly Regex NumberRegex = new Regex(@"\G\s*(?<v>(\d*\.)?\d+)\s*", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex BoolRegex = new Regex(@"\G\s*(?<v>true|false)\s*", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex NullRegex = new Regex(@"\G\s*null\s*", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex StringRegex = new Regex(@"\G\s*(?<q>['""])(?<v>((?!\k<q>)(\\\k<q>|.))*)\k<q>\s*", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex LabelRegex = new Regex(@"\G\s*(?<v>\w+)\s*", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex OtherTokenRegex = new Regex(@"\G\s*(?<v>[\[\]{},])\s*", RegexOptions.Compiled | RegexOptions.Singleline);

        public static object ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            int index = 0;
            StringPool strings = null;

            return ParseValue(json, ref index, ref strings);
        }

        private static JavaScriptObject ParseObject(string s, ref int index, ref StringPool strings)
        {
            var jsonObject = new JavaScriptObject();

            while (index < s.Length)
            {
                string label;

                var match = StringRegex.Match(s, index);
                if (match.Success)
                {
                    label = Unescape(match.Groups["v"].Value);
                }
                else
                {
                    match = LabelRegex.Match(s, index);
                    if (!match.Success)
                        throw new InvalidOperationException();

                    label = match.Groups["v"].Value;
                }

                if (strings == null)
                    strings = new StringPool();

                label = strings.Intern(label);

                index += match.Length;
                if (index >= s.Length || s[index] != ':')
                    throw new InvalidOperationException();

                index++;

                var value = ParseValue(s, ref index, ref strings);

                jsonObject.Add(label, value);

                match = OtherTokenRegex.Match(s, index);
                if (!match.Success)
                    throw new InvalidOperationException();

                index += match.Length;

                var delimeter = match.Groups["v"].Value[0];
                if (delimeter == '}')
                    return jsonObject;
                else if (delimeter != ',')
                    throw new InvalidOperationException();
            }

            throw new InvalidOperationException();
        }
        private static object ParseValue(string s, ref int index, ref StringPool strings)
        {
            var match = NumberRegex.Match(s, index);
            if (match.Success)
            {
                index += match.Length;

                var value = match.Groups["v"].Value;
                int intValue;
                if (int.TryParse(value, out intValue))
                    return intValue;

                double doubleValue;
                if (double.TryParse(value, out doubleValue))
                    return doubleValue;

                throw new InvalidOperationException();
            }

            match = StringRegex.Match(s, index);
            if (match.Success)
            {
                index += match.Length;

                if (strings == null)
                    strings = new StringPool();

                return strings.Intern(Unescape(match.Groups["v"].Value));
            }

            match = BoolRegex.Match(s, index);
            if (match.Success)
            {
                index += match.Length;
                var value = match.Groups["v"].Value;

                return bool.Parse(value);
            }

            match = NullRegex.Match(s, index);
            if (match.Success)
            {
                index += match.Length;
                return null;
            }

            match = OtherTokenRegex.Match(s, index);
            if (match.Success)
            {
                var token = match.Groups["v"].Value[0];
                index += match.Length;
                if (token == '[')
                {
                    return ParseArray(s, ref index, ref strings);
                }
                else if (token == '{')
                {
                    return ParseObject(s, ref index, ref strings);
                }
                else
                    throw new InvalidOperationException();
            }

            throw new InvalidOperationException();
        }
        private static object ParseArray(string s, ref int index, ref StringPool strings)
        {
            var values = new JavaScriptArray();
            bool first = true;

            while (index < s.Length)
            {
                var match = OtherTokenRegex.Match(s, index);
                if (match.Success && match.Groups["v"].Value[0] == ']')
                {
                    index += match.Length;
                    return values;
                }
                else if (!first)
                {
                    if (match.Success && match.Groups["v"].Value[0] == ',')
                        index += match.Length;
                    else
                        throw new InvalidOperationException();
                }

                values.Add(ParseValue(s, ref index, ref strings));
                first = false;
            }

            throw new InvalidOperationException();
        }
        private static string Unescape(string s)
        {
            return Regex.Replace(
                s,
                @"\\.",
                m =>
                {
                    switch (m.Value.ToLower())
                    {
                        case @"\n": return "\n";
                        case @"\r": return "\r";
                        case @"\t": return "\t";
                        case @"\\": return "\\";
                        case @"\'": return "\'";
                        case @"\""": return "\"";
                        default: return m.Value;
                    }
                }
            );
        }

        private sealed class StringPool : Dictionary<string, string>
        {
            public StringPool()
            {
            }

            public string Intern(string s)
            {
                if (s == null)
                    return null;
                if (s == string.Empty)
                    return string.Empty;

                string value;
                if (this.TryGetValue(s, out value))
                    return value;

                this.Add(s, s);
                return s;
            }
        }
    }
}
