using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support.Discord.Services
{
    internal static class LocalizationService
    {
        public static Dictionary<string, Dictionary<string, string>> localizations =
            JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText("localization.json"))
            ?? new Dictionary<string, Dictionary<string, string>>();

        public static Dictionary<string, string> GetLocalized(string key)
        {
            return localizations.ContainsKey(key) 
                ? localizations[key] 
                : new() { { "en-US", key } };
        }

        public static string GetLocalized(string key, string locale)
        {
            return GetLocalized(key).ContainsKey(locale) 
                ? GetLocalized(key)[locale] 
                : GetLocalized(key)["en-US"];
        }
    }
}
