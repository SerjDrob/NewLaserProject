namespace NewLaserProject.Classes
{
    internal interface ISettingsManager<TSettings>
    {
        TSettings Settings { get; }
        void Save();
        void SetSettings(TSettings settings);
        void Load();
    }
}
