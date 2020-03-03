using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TechTalk.SpecFlow;

namespace EventDrivenThinking.Tests.Common
{
    class NamedGuidConverter : JsonConverter<Guid>
    {
        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
        {
            JToken.FromObject(value.ToString()).WriteTo(writer);
        }

        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string txt = JToken.Load(reader).ToString();
            return txt.ToGuid();
        }
    }
    public static class TableExtensions
    {
        private static List<JsonConverter> _converters;
        private static JsonSerializerSettings _settings;
        static TableExtensions()
        {
            _converters = new List<JsonConverter>();
            _converters.Add(new NamedGuidConverter());
            _settings = new JsonSerializerSettings() { Converters = _converters };
        }
        public static object Deserialize(this Table t, Type objectType, Guid? id = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");
            var items = t.Rows.Select(r => $"\"{StringDehumanizeExtensions.Dehumanize(r[0])}\": {TryQuote(r[1])}")
                .ToList();
            if (id.HasValue)
                items.Add($"\"Id\": \"{id.Value}\"");

            sb.Append(string.Join($",{Environment.NewLine}", items));
            sb.Append("}");

            string json = sb.ToString();
            return JsonConvert.DeserializeObject(json,objectType, _settings);
        }

        private static string TryQuote(string obj)
        {
            if (obj.StartsWith('['))
                return obj;
            else return $"\"{obj}\"";
        }
    }
}