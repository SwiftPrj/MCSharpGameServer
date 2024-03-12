using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LegacyServer
{
    public static class JSONParser
    {
        public static Dictionary<string, float> Decode(string json)
        {
            string[] cache = json.Replace("{", String.Empty).Replace("}", String.Empty).Replace("\"", String.Empty).Trim().Split(",");
            Dictionary<string, float> temp = new Dictionary<string, float>();
            foreach (string line in cache)
            {
                string key = line.Split(":")[0].Trim();
                float value = float.Parse(line.Split(":")[1].Trim());
                temp.Add(key, value);
            }
            return temp;
        }

        public static Dictionary<string, string> DecodeString(string json)
        {
            string[] cache = json.Replace("{", String.Empty).Replace("}", String.Empty).Replace("\"", String.Empty).Trim().Split(",");
            Dictionary<string, string> temp = new Dictionary<string, string>();
            foreach (string line in cache)
            {
                string key = line.Split(":")[0].Trim();
                string value = line.Split(":")[1].Trim();
                temp.Add(key, value);
            }
            return temp;
        }

        public static string EncodeString(Dictionary<string, string> dict) 
        {
            return JsonSerializer.Serialize(dict);
        }

        public static string Encode(Dictionary<string, float> dict) 
        {
            return JsonSerializer.Serialize(dict);
        }
    }
}
