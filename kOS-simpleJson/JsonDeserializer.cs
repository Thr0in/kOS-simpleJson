using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using System;
using UnityEngine;
using JsonArray = kOS.Safe.JsonArray;
using JsonObject = kOS.Safe.JsonObject;

namespace kOS.AddOns.Json
{
    public class JsonDeserializer
    {
        private static readonly JsonDeserializer instance;

        public static JsonDeserializer ReaderInstance
        {
            get
            {
                return instance;
            }
        }

        private JsonDeserializer()
        { }

        static JsonDeserializer()
        {
            instance = new JsonDeserializer();
        }

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
        /// Converts a deserialized JSON token into a kOS <see cref="Structure"/> instance.
        /// Handles JSON objects, arrays, strings, numbers and booleans and provides robust handling for numeric ranges.
        /// </summary>
        /// <param name="obj">The deserialized JSON value to convert.</param>
        /// <returns>A kOS <see cref="Structure"/> representation of the input value.</returns>
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
        /// Converts the specified JSON object to a Kos Lexicon, mapping each key-value pair to the corresponding
        /// Lexicon entry.
        /// </summary>
        /// <param name="jsonObject">The JSON object containing key-value pairs to be converted. Cannot be null.</param>
        /// <returns>A Lexicon instance containing entries for each key in the JSON object, with values converted to Kos
        /// structures.</returns>
        private Lexicon ToKosLexicon(JsonObject jsonObject)
        {
            Lexicon result = new Lexicon();
            foreach (var key in jsonObject.Keys)
            {
                Debug.Log("Key: " + key + " value: " + jsonObject[key] + " type: " + jsonObject[key].GetType());
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
                Debug.Log("Item: " + item + " type: " + item.GetType());
                result.Add(ToKosStructure(item));
            }
            return result;
        }

        /// <summary>
        /// Deserializes a JSON-formatted string into an object representing the corresponding JSON value.
        /// </summary>
        /// <remarks>The returned object type depends on the structure of the input JSON string. If the
        /// input does not match a recognized JSON type, the method returns null.</remarks>
        /// <param name="input">The JSON string to deserialize. Leading and trailing whitespace is ignored. Cannot be null or empty.</param>
        /// <returns>An object representing the deserialized JSON value. The return type may be a JsonObject for JSON objects, a
        /// JsonArray for arrays, a string for JSON strings, or a Boolean for the literals "true" and "false". Returns
        /// null if the input is "null" or if the input does not match a recognized JSON type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
        private object ParseJsonString(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            input = input.Trim();
            if (input.Length == 0)
                return input;

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

                    return input;
            }
        }
    }
}
