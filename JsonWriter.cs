using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace Inedo.BuildMasterExtensions.LeanKit
{
    internal static class JsonWriter
    {
        private static readonly Type[] NumericTypes = new[]
            {
                typeof(int), typeof(double), typeof(short), typeof(byte), typeof(long),
                typeof(float), typeof(sbyte), typeof(ushort), typeof(uint), typeof(ulong),
                typeof(decimal)
            };
        private static readonly Regex EscapeRegex = new Regex(@"(?<q>[""'\\])", RegexOptions.Compiled);

        /// <summary>
        /// Writes an object as JSON-serialized data to a <see cref="System.IO.TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/> used to write the JSON data.</param>
        /// <param name="obj">The object to serialize.</param>
        public static void WriteJson(TextWriter writer, object obj)
        {
            if (obj == null || Convert.IsDBNull(obj))
            {
                writer.Write("null");
                return;
            }

            var objType = obj.GetType();

            if (Array.IndexOf(NumericTypes, objType) >= 0)
            {
                writer.Write(obj);
            }
            else if (objType == typeof(bool))
            {
                writer.Write((bool)obj ? "true" : "false");
            }
            else if (objType == typeof(string))
            {
                writer.Write('\"');
                writer.Write(JsonEncode(obj.ToString()));
                writer.Write('\"');
            }
            else if (objType == typeof(JavaScriptLiteral))
            {
                writer.Write(((JavaScriptLiteral)obj).LiteralText);
            }
            else if (objType == typeof(System.DateTime))
            {
                writer.Write('\"');
                writer.Write(((System.DateTime)obj).ToString("o"));
                writer.Write('\"');
            }
            else if (typeof(IDictionary).IsAssignableFrom(objType))
            {
                WriteJsonDictionary(writer, (IDictionary)obj);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(objType))
            {
                writer.Write('[');

                bool first = true;
                foreach (var item in (IEnumerable)obj)
                {
                    if (!first)
                        writer.Write(',');

                    WriteJson(writer, item);

                    first = false;
                }

                writer.Write(']');
            }
            else
            {
                var props = objType.GetProperties();
                if (props.Length == 0)
                {
                    writer.Write('\"');
                    writer.Write(JsonEncode(obj.ToString()));
                    writer.Write('\"');
                    return;
                }
                else
                {
                    WriteJsonObject(writer, obj);
                }
            }
        }
        /// <summary>
        /// Returns a JSON-format string of a serialized object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>JSON-formatted string representation of the object.</returns>
        public static string ToJson(object obj)
        {
            var buffer = new StringWriter();
            WriteJson(buffer, obj);
            return buffer.ToString();
        }
        /// <summary>
        /// Escapes characters necessary to encode the string in JSON format.
        /// </summary>
        /// <param name="s">String to encode.</param>
        /// <returns>Encoded string.</returns>
        public static string JsonEncode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            var escaped = EscapeRegex.Replace(s, @"\${q}");
            return escaped.Replace("\t", @"\t")
                          .Replace("\r", @"\r")
                          .Replace("\n", @"\n");
        }
        /// <summary>
        /// Returns an instance representing literal JavaScript.
        /// </summary>
        /// <param name="s">The literal.</param>
        /// <returns>Instance representing the literal.</returns>
        public static JavaScriptLiteral Literal(string s)
        {
            return new JavaScriptLiteral(s);
        }


        private static void WriteJsonObject(TextWriter writer, object obj)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            if (obj == null)
            {
                writer.Write("null");
                return;
            }

            writer.Write('{');
            bool first = true;
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (!first)
                    writer.Write(',');

                writer.Write('\"');
                writer.Write(prop.Name);
                writer.Write("\":");
                WriteJson(writer, prop.GetValue(obj, null));

                first = false;
            }
            writer.Write('}');
        }
        private static void WriteJsonDictionary(TextWriter writer, IDictionary obj)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            if (obj == null)
            {
                writer.Write("null");
                return;
            }

            writer.Write('{');
            bool first = true;
            foreach (DictionaryEntry prop in obj)
            {
                if (!first)
                    writer.Write(',');

                writer.Write('\"');
                writer.Write(prop.Key);
                writer.Write("\":");
                WriteJson(writer, prop.Value);

                first = false;
            }
            writer.Write('}');
        }

        /// <summary>
        /// Represents literal JavaScript text.
        /// </summary>
        public sealed class JavaScriptLiteral
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="JavaScriptLiteral"/> class.
            /// </summary>
            /// <param name="literal">The literal.</param>
            public JavaScriptLiteral(string literal)
            {
                this.LiteralText = literal ?? string.Empty;
            }

            /// <summary>
            /// Gets the literal text.
            /// </summary>
            public string LiteralText { get; private set; }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return this.LiteralText;
            }
        }
    }
}
