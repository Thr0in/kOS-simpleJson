using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;

namespace kOS.AddOns.Json
{
    [kOSAddon("JSON")]
    [kOS.Safe.Utilities.KOSNomenclature("JSONAddon")]
    public class SimpleJsonAddon : Suffixed.Addon
    {
        public SimpleJsonAddon(SharedObjects shared) : base(shared)
        {
            InitializeSufixes();
        }

        public override BooleanValue Available()
        {
            return true;
        }

        private void InitializeSufixes()
        {
            AddSuffix("STRINGIFY", new OneArgsSuffix<StringValue, Structure>(Stringify, "Get a json string for an object"));
            AddSuffix("PARSE", new OneArgsSuffix<Structure, StringValue>(Parse, "Get an object from a json string"));
        }

        private StringValue Stringify(Structure obj)
        {
            SerializableStructure serialized = obj as SerializableStructure;

            if (serialized == null)
            {
                throw new KOSException("This type is not serializable");
            }
            string serializedString = new SafeSerializationMgr(shared).Serialize(serialized, SimpleJsonFormatter.WriterInstance, false);
            return new StringValue(serializedString);
        }

        private Structure Parse(StringValue json)
        {
            try
            {
                return JsonDeserializer.ReaderInstance.Deserialize(json);
            }
            catch (ArgumentNullException)
            {
                throw new KOSInvalidArgumentException("PARSE","json" , "The provided JSON string is null");
            }
        }
    }
}
