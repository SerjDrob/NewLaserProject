using System;

namespace NewLaserProject.Classes
{
    internal interface ISettingsManager<TSettings>: IObservable<TSettings>, IDisposable
    {
        TSettings Settings { get; }
        void Save();
        void SetSettings(TSettings settings);
        void Load();
    }
}
