namespace Game.Core.Save
{
    /// <summary>
    /// Persistence abstraction. New code must use this instead of PlayerPrefs directly.
    /// </summary>
    public interface ISaveService
    {
        void SaveInt(string key, int value);
        int LoadInt(string key, int defaultValue = 0);
        void SaveString(string key, string value);
        string LoadString(string key, string defaultValue = "");
        bool HasKey(string key);
        void DeleteKey(string key);
        void Save();
    }
}
