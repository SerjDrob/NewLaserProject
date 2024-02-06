using System;

namespace NewLaserProject.Classes
{
    internal class SettingsManager<T> : ISettingsManager<T>
    {
        private readonly string _settingsPath;
        public T? Settings
        {
            get;
            private set;
        }
        public SettingsManager(string settingsPath) => _settingsPath = settingsPath;
        public void Load()
        {
            try
            {
                var deserializer = new JsonDeserializer<T>();
                Settings = deserializer.DeserializeFromFile(_settingsPath);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public void Save()
        {
            try
            {
                Settings?.SerializeObject(_settingsPath);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void SetSettings(T settings)
        {
            Settings = settings;
        }
    }
}
