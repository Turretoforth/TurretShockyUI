using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TurretShockyUI.Models
{
    public static class Preferences
    {
        private const string PrefsPath = "prefs.json";
        private readonly static object lockObj = new();
        private static Dictionary<string, object> _prefsCache = [];

        public static void Initialize()
        {
            if (!File.Exists(PrefsPath))
            {
                File.WriteAllText(PrefsPath, JsonSerializer.Serialize(new Dictionary<string, object>()));
            }
            _prefsCache = LoadPreferences();
        }

        private static Dictionary<string, object> LoadPreferences()
        {
            string json = File.ReadAllText(PrefsPath);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? [];
        }

        private static void SavePreferences(Dictionary<string, object> prefs)
        {
            string json = JsonSerializer.Serialize(prefs);
            File.WriteAllText(PrefsPath, json);
        }

        public static T? Get<T>(string key, T defaultValue)
        {
            lock (lockObj)
            {
                if (_prefsCache.TryGetValue(key, out var value))
                {
                    return ((JsonElement)value).Deserialize<T>();
                }
                else
                {
                    Set(key, defaultValue);
                    return defaultValue;
                }
            }
        }

        public static void Set<T>(string key, T value)
        {
            lock (lockObj)
            {
                if (_prefsCache.ContainsKey(key))
                {
                    _prefsCache[key] = JsonSerializer.SerializeToElement(value!);
                }
                else
                {
                    _prefsCache.Add(key, JsonSerializer.SerializeToElement(value!));
                }
                SavePreferences(_prefsCache);
            }
        }
    }
}