using UnityEngine;

namespace Game.Core.Save
{
    /// <summary>
    /// Default local persistence via PlayerPrefs. Swap later for JSON/cloud without changing callers.
    /// </summary>
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        public void SaveInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public int LoadInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value ?? string.Empty);
        }

        public string LoadString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue ?? string.Empty);
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
