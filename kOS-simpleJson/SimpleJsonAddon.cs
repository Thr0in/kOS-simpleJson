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
            AddSuffix("PARSEORELSE", new TwoArgsSuffix<Structure, StringValue, Structure>(ParseOrElse, "Get an object from a json string, or else return"));
            AddSuffix("PARSEORELSEGET", new TwoArgsSuffix<Structure, StringValue, KOSDelegate>(ParseOrElseGet, "Get an object from a json string or else call a delegate and return its value."));
            AddSuffix("ISPARSEABLE", new OneArgsSuffix<BooleanValue, StringValue>(IsParseable, "Returns true if the string can be parsed as json"));
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

        private Structure ParseOrElse(StringValue json, Structure elseValue)
        {
            try
            {
                return Parse(json);
            }
            catch (Exception)
            {
                return elseValue;
            }
        }

        private Structure ParseOrElseGet(StringValue json, KOSDelegate elseFunc)
        {
            try
            {
                return Parse(json);
            }
            catch (Exception)
            {
                return elseFunc.CallWithArgsPushedAlready();
            }
        }

        private BooleanValue IsParseable(StringValue json)
        {
            try
            {
                Parse(json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
