using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using UnityEngine;

namespace kOS.AddOns.Json
{
    [kOSAddon("JSON")]
    [kOS.Safe.Utilities.KOSNomenclature("JSONAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base(shared)
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
            return UnityEngine.JsonUtility.ToJson(obj);
        }

        private Structure Parse(StringValue json)
        {
            return JsonUtility.FromJson<Structure>(json);
        }
    }
}
