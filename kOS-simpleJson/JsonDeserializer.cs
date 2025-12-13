using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using System;
using System.Runtime.Serialization;
using JsonArray = kOS.Safe.JsonArray;
using JsonObject = kOS.Safe.JsonObject;

namespace kOS.AddOns.Json
{
    public class JsonDeserializer
    {
        private static readonly JsonDeserializer instance = new JsonDeserializer();

        public static JsonDeserializer ReaderInstance
        {
            get
            {
                return instance;
            }
        }

        private JsonDeserializer()
        { }

        /// <summary>
        /// Parses the specified input string and returns its corresponding structured representation.
        /// </summary>
        /// <param name="input">The input string to be parsed. Must be a valid serialized structure; otherwise, parsing may fail.</param>
        /// <returns>A <see cref="Structure"/> instance representing the parsed data from the input string.</returns>
        public Structure Deserialize(string input)
        {
            return ToKosStructure(ParseJsonString(input));
        }

        /// <summary>
        /// Determines whether the specified string can be successfully parsed as a valid JSON value.
        /// </summary>
        /// <remarks>This method does not throw an exception for invalid input. It returns false if the
        /// input cannot be parsed due to format errors or unsupported types.</remarks>
        /// <param name="input">The string to test for JSON parseability. May be null. If <c>null</c>, returns false.</param>
        /// <returns>true if the input string can be parsed as valid JSON; otherwise, false.</returns>
        public static BooleanValue IsParseable(string input)
        {
            try
            {
                ParseJsonString(input);
                return new BooleanValue(true);
            }
            catch (Exception)
            {
                return new BooleanValue(false);
            }
        }

        /// <summary>
        /// Converts a deserialized JSON token into a kOS <see cref="Structure"/> instance.
        /// Handles JSON objects, arrays, strings, numbers and booleans and provides robust handling for numeric ranges.
        /// </summary>
        /// <param name="obj">The deserialized JSON value to convert.</param>
        /// <returns>A kOS <see cref="Structure"/> representation of the input value.</returns>
        /// <exception cref="KOSSerializationException">Thrown when the input can't be converted to any kOS structure.</exception>
        private Structure ToKosStructure(object obj)
        {
            if (obj == null)
                return new StringValue("");

            switch (obj)
            {
                case JsonObject jsonObject:
                    return ToKosLexicon(jsonObject);

                case JsonArray jsonArray:
                    return ToKosList(jsonArray);

                case string s:
                    return new StringValue(s);

                case int i:
                    return new ScalarIntValue(i);

                case long l:
                    if (l >= int.MinValue && l <= int.MaxValue)
                        return new ScalarIntValue((int)l);
                    return new ScalarDoubleValue((double)l);

                case double d:
                    return new ScalarDoubleValue(d);

                case float f:
                    return new ScalarDoubleValue(f);

                case decimal m:
                    return new ScalarDoubleValue((double)m);

                case bool b:
                    return new BooleanValue(b);

                default:
                    throw new KOSSerializationException("Original value failed to deserialize. Please create a bug report. " + obj);
            }
        }

        /// <summary>
        /// Converts the specified <see cref="JsonObject"/>> to a Kos <see cref="Lexicon"/>, mapping each key-value pair to the corresponding
        /// Lexicon entry.
        /// </summary>
        /// <param name="jsonObject">The JSON object containing key-value pairs to be converted. Cannot be null.</param>
        /// <returns>A <see cref="Lexicon"/> instance containing entries for each key in the JSON object, with values converted to Kos
        /// structures.</returns>
        private Lexicon ToKosLexicon(JsonObject jsonObject)
        {
            Lexicon result = new Lexicon();
            foreach (var key in jsonObject.Keys)
            {
                result[new StringValue(key)] = ToKosStructure(jsonObject[key]);
            }
            return result;
        }

        /// <summary>
        /// Converts a <see cref="JsonArray"/> to a <see cref="ListValue"/> by transforming each element into its
        /// corresponding structure.
        /// </summary>
        /// <param name="jsonArray">The JSON array containing elements to be converted. Cannot be null.</param>
        /// <returns>A <see cref="ListValue"/> containing the converted elements from the specified JSON array. The list will be
        /// empty if the array contains no elements.</returns>
        private ListValue ToKosList(JsonArray jsonArray)
        {
            ListValue result = new ListValue();
            foreach (var item in jsonArray)
            {
                result.Add(ToKosStructure(item));
            }
            return result;
        }

        /// <summary>
        /// Deserializes a JSON-formatted string into an object representing the corresponding JSON value.
        /// </summary>
        /// <remarks>
        /// This method parses JSON strings and returns the corresponding object representation.
        /// Leading and trailing whitespace is automatically trimmed before parsing.
        /// Supported JSON types include objects, arrays, strings, numbers, booleans, and null.
        /// Primitive values (numbers and booleans) that are not enclosed in quotes are also supported.
        /// </remarks>
        /// <param name="input">The JSON string to deserialize. Cannot be null or empty after trimming whitespace.</param>
        /// <returns>
        /// An object representing the deserialized JSON value:
        /// <list type="bullet">
        /// <item><description><see cref="JsonObject"/> for JSON objects (e.g., <c>{"key": "value"}</c>)</description></item>
        /// <item><description><see cref="JsonArray"/> for JSON arrays (e.g., <c>[1, 2, 3]</c>)</description></item>
        /// <item><description><see cref="string"/> for JSON strings (e.g., <c>"text"</c>)</description></item>
        /// <item><description><see cref="int"/> or <see cref="double"/> for JSON numbers (e.g., <c>42</c>, <c>3.14</c>)</description></item>
        /// <item><description><see cref="bool"/> for JSON booleans (e.g., <c>true</c>, <c>false</c>)</description></item>
        /// <item><description><c>null</c> for the JSON literal <c>null</c></description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the input string is empty after trimming, or when the input does not represent a valid JSON value.</exception>
        /// <exception cref="SerializationException">Thrown when the JSON string is malformed or contains invalid syntax.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the JSON contains invalid Unicode escape sequences (e.g., invalid surrogate pairs).</exception>
        /// <exception cref="InvalidCastException">Thrown when the deserialized object cannot be cast to the expected type.</exception>
        private static object ParseJsonString(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            input = input.Trim();
            if (input.Length == 0)
                throw new ArgumentException("Input string is empty. An empty string is not valid JSON.");

            string first = input.Substring(0, 1);
            switch (first)
            {
                case "{":
                    return SimpleJson.DeserializeObject<JsonObject>(input);

                case "[":
                    return SimpleJson.DeserializeObject<JsonArray>(input);

                case "\"":
                    return SimpleJson.DeserializeObject<string>(input);

                default:
                    if (input == "true" || input == "false")
                        return input == "true";

                    if (int.TryParse(input, out int intNumber))
                        return intNumber;
                    if (double.TryParse(input, out double number))
                        return number;

                    if (input == "null")
                        return null;

                    throw new ArgumentException($"Input is not valid JSON: '{input}'");
            }
        }
    }
}
