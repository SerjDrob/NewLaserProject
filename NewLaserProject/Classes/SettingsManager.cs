using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLaserProject.Properties;

namespace NewLaserProject.Classes
{

    internal interface ISettingsManager<TSettings>
    {
        TSettings Settings { get; }
        void Save();
        void Load();
    }
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
    }

    internal static class SettingsManagerExtensions
    {
        public static K GetNonNull<T, K>(this ISettingsManager<T> settingsManager, Func<T, K> getProp)
        {
            var prop = getProp(settingsManager.Settings) ?? throw new ArgumentNullException("The property is null");
            return (K)prop;
        }
    }

    internal class LaserMachineSettings
    {
        public double? XVelLow { get; set; }
        public double? XVelHigh { get; set; }
        public double? XAcc { get; set; }
        public double? XDec { get; set; }
        public int? XPPU { get; set; }
        public int? XJerk { get; set; }
        public double? YVelHigh { get; set; }
        public double? YAcc { get; set; }
        public double? YDec { get; set; }
        public int? YPPU { get; set; }
        public int? YJerk { get; set; }
        public double? ZVelLow { get; set; }
        public double? ZVelHigh { get; set; }
        public double? ZAcc { get; set; }
        public double? ZDec { get; set; }
        public int? ZPPU { get; set; }
        public int? ZJerk { get; set; }
        //public double? UVelLow { get; set; }
        //public double? UVelHigh { get; set; }
        //public double? UAcc { get; set; }
        //public double? UDec { get; set; }
        //public int? UPPU { get; set; }
        //public int? UJerk { get; set; }
        public double? XVelService { get; set; }
        public double? YVelService { get; set; }
        public double? ZVelService { get; set; }
        //public double? UVelService { get; set; }
        public double? YVelLow { get; set; }
        //public double? ZObjective { get; set; }
        //public bool? CoolantSensorDsbl { get; set; }
        public double? XLoad { get; set; }
        public double? YLoad { get; set; }
        public double? CameraScale { get; set; }
        public double? XOffset { get; set; }
        public double? YOffset { get; set; }
        public double? XRightPoint { get; set; }
        public double? YRightPoint { get; set; }
        public double? XLeftPoint { get; set; }
        public double? YLeftPoint { get; set; }
        public double? ZeroFocusPoint { get; set; }
        public double? ZeroPiercePoint { get; set; }
        public bool? WaferMirrorX { get; set; }
        public bool? WaferAngle90 { get; set; }
        public double? XNegDimension { get; set; }
        public double? XPosDimension { get; set; }
        public double? YNegDimension { get; set; }
        public double? YPosDimension { get; set; }
        public int? DefaultWidth { get; set; }
        public int? DefaultHeight { get; set; }
        public bool? IsMirrored { get; set; }
        //public bool? IsRotated { get; set; }
        public double? WaferWidth { get; set; }
        public double? WaferHeight { get; set; }
        public double? WaferThickness { get; set; }
        public int? PreferredCameraCapabilities { get; set; }
        public double? PazAngle { get; set; }
    }
}
