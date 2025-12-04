using kOS.Safe;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JsonObject = kOS.Safe.JsonObject;

namespace kOS.AddOns.Json
{
    public class SimpleJsonFormatter : IFormatWriter
    {
        private static readonly SimpleJsonFormatter instance;

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