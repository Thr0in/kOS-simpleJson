using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using JsonArray = kOS.Safe.JsonArray;
using JsonObject = kOS.Safe.JsonObject;

namespace kOS.AddOns.Json
{
    public class SimpleJsonFormatter : IFormatWriter
    {
        private static readonly SimpleJsonFormatter instance;

        public static SimpleJsonFormatter ReaderInstance
        {
            get
            {
                return instance;
            }
        }

        public static IFormatWriter WriterInstance
        {
            get
            {
                return instance;
            }
        }

        private SimpleJsonFormatter()
        { }

        static SimpleJsonFormatter()
        {
            instance = new SimpleJsonFormatter();
        }

        /// <summary>
        /// Serializes the specified dump object to its string representation.
        /// </summary>
        /// <param name="dump">The dump object to serialize. Cannot be null.</param>
        /// <returns>A string containing the serialized representation of the dump object.</returns>
        public string Write(Dump dump)
        {
            return Serialize(dump); 
        }

        /// <summary>
        /// Parses the specified input string and returns its corresponding structured representation.
        /// </summary>
        /// <param name="input">The input string to be parsed. Must be a valid serialized structure; otherwise, parsing may fail.</param>
        /// <returns>A <see cref="Structure"/> instance representing the parsed data from the input string.</returns>
        public Structure Read(string input)
        {
            return ToKosStructure(Deserialize(input));
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
        private object Deserialize(string input)
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

        /// <summary>
        /// Serializes the specified kOS JSON dump into a JSON string representation based on its structure.
        /// </summary>
        /// <param name="dump">The kOS JSON dump to serialize. Must contain a recognized key indicating the data type ('value', 'Items', or
        /// 'Entries').</param>
        /// <returns>A JSON string representing the serialized contents of the dump. Returns an empty array ('[]') or object
        /// ('{}') if the corresponding value is not a list.</returns>
        /// <exception cref="KOSSerializationException">Thrown if the dump does not contain a value for the expected key, or if the key is not recognized as a valid
        /// kOS JSON type.</exception>
        private string Serialize(Dump dump)
        {
            // Filter out the $type key used in kOS JSON dumps
            var keys = dump.Keys.Where(k => k as string != "$type");

            // In case of multiple keys, we serialize the entire dictionary
            // Needed for pidloops and ranges
            if (keys.Count() > 1)
            {
                return SerializeDictionary(dump);
            }

            // All kOS JSON dumps should have exactly one key (other than $type)
            var key = keys.ElementAt(0);
            if (!dump.TryGetValue(key, out object value))
                throw new KOSSerializationException("No value present in kOS JSON data: " + key);

            switch (key)
            {
                // All primitive values are stored under "value"
                case "value":
                    return SimpleJson.SerializeObject(value);

                // All array like structures are stored under "Items"
                case kOS.Safe.Dump.Items:
                    if (value is List<object>)
                    {
                        return SerializeArrayLike(value as List<object>);
                    }
                    return "[]";

                // All object like structures are stored under "Entries"
                case kOS.Safe.Dump.Entries:
                    if (value is List<object>)
                    {
                        return SerializeObjectLike(value as List<object>);
                    }
                    return "{}";

                default:
                    throw new KOSSerializationException("Invalid key in kOS JSON data: " + key + " \n Please create a bug report.");
            }
        }

        /// <summary>
        /// Serializes a list of objects into a JSON-like array string representation.
        /// </summary>
        /// <param name="list">The list of objects to serialize. Each element should be compatible with the expected serialization format.</param>
        /// <returns>A string representing the serialized array in JSON-like format. The result includes each element's
        /// serialized value, separated by commas and enclosed in square brackets.</returns>
        private string SerializeArrayLike(List<object> list)
        {
            List<string> newList = new List<string>();
            foreach (var element in list)
            {
                newList.Add(Serialize(element as Dump));
            }
            return $"[{string.Join(",", newList)}]";
        }

        /// <summary>
        /// Serializes a list of key-value pairs into a string representation resembling an object, with each key and
        /// value formatted as "key:value" pairs.
        /// </summary>
        /// <remarks>The input list must contain an even number of elements, with keys at even indices and
        /// corresponding values at odd indices. This method is intended for internal serialization scenarios where
        /// object-like structures are represented as flat lists.</remarks>
        /// <param name="keyValueList">A list containing alternating key and value objects. Keys must be serializable to strings; values are
        /// serialized using the same logic.</param>
        /// <returns>A string representing the serialized object-like structure, with key-value pairs separated by commas and
        /// enclosed in curly braces.</returns>
        /// <exception cref="KOSSerializationException">Thrown if a key in the list cannot be serialized to a string.</exception>
        private string SerializeObjectLike(List<object> keyValueList)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < keyValueList.Count(); i += 2)
            {
                string objectKey = Serialize(keyValueList[i] as Dump) as string;
                if (objectKey == null)
                    throw new KOSSerializationException("Key of object-like is not a string: " + objectKey);
                result.Add($"{objectKey}:{Serialize(keyValueList[i + 1] as Dump)}");
            }
            return "{" + string.Join(",", result) + "}";
        }

        /// <summary>
        /// Serializes the specified dictionary into a JSON string representation.
        /// </summary>
        /// <remarks>If a value in the dictionary is itself a dictionary, it will be serialized
        /// recursively. Keys are converted to their string representation using ToString().
        /// At the time of creation this is only needed for serializing pidloops and ranges.</remarks>
        /// <param name="dict">The dictionary containing key-value pairs to serialize. Keys are converted to strings; values may be
        /// serialized recursively if they are dictionaries.</param>
        /// <returns>A JSON-formatted string representing the contents of the dictionary.</returns>
        private string SerializeDictionary(Dictionary<object, object> dict)
        {
            JsonObject result = new JsonObject();
            foreach (var keyValue in dict)
            {
                result[keyValue.Key.ToString()] = keyValue.Value is IDictionary ? Serialize(keyValue.Value as Dump) : keyValue.Value;
            }
            return SimpleJson.SerializeObject(result);
        }
    }
}