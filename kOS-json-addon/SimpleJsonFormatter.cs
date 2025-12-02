using JetBrains.Annotations;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using UnityEngine;
using static FileIO;
using static GameEvents;
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
                throw new ArgumentNullException(nameof(obj));

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
                    return new StringValue("Original value failed to deserialize");
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

        private object Deserialize(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            input = input.TrimStart();
            if (input.Length == 0)
                throw new KOSInvalidArgumentException("placeholder", "propagate through later", "Input is empty");

            object deserialized = null;

            string first = input.Substring(0, 1);
            switch (first)
            {
                case "{":
                    deserialized = SimpleJson.DeserializeObject<JsonObject>(input);
                    break;

                case "[":
                    deserialized = SimpleJson.DeserializeObject<JsonArray>(input);
                    break;

                case "\"":
                    deserialized = SimpleJson.DeserializeObject<string>(input);
                    break;

                default:
                    if (input == "true" || input == "false")
                        deserialized = input == "true";

                    if (input == "null")
                        Debug.Log("Parsed JSON data is null");

                    Debug.Log("Fell through");
                    break;
            }
            return deserialized;
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
            object value = null;
            var keys = dump.Keys.Where(k => k != "$type");// First();

            if (keys.Count() > 1)
            {
                return SerializeDictionary(dump as Dictionary<object, object>);
            }
            var key = keys.ElementAt(0);

            if (!dump.TryGetValue(key, out value))
                throw new KOSSerializationException("No value present in kOS JSON data: " + key);

            switch (key)
            {
                case "value":
                    return SimpleJson.SerializeObject(value);

                case kOS.Safe.Dump.Items:
                    if (value is List<object>)
                    {
                        return SerializeArrayLike(value as List<object>);
                    }
                    return "[]";

                case kOS.Safe.Dump.Entries:
                    if (value is List<object>)
                    {
                        return SerializeObjectLike(value as List<object>);
                    }
                    return "{}";

                default:
                    throw new KOSSerializationException("Invalid key in kOS JSON data: " + key);
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

        private string SerializeDictionary(Dictionary<object, object> dict)
        {
            JsonObject result = new JsonObject();
            foreach (var keyValue in dict)
            {
                result[keyValue.Key.ToString()] = keyValue.Value;
            }
            return SimpleJson.SerializeObject(result);
        }
    }
}