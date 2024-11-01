using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutoMapper;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine.Machines;
using NewLaserProject.Properties;
using NewLaserProject.ViewModels.DialogVM;
using Newtonsoft.Json;

namespace NewLaserProject.Classes
{
    internal static class ExtensionMethods
    {
        internal static void CopyToSettings(this MachineSettingsVM machineSettings)
        {
            Settings.Default.XAcc = machineSettings.XAcc;
            //Settings.Default.XLoad
            Settings.Default.XVelHigh = machineSettings.XVelHigh;
            Settings.Default.XVelLow = machineSettings.XVelLow;
            Settings.Default.XVelService = machineSettings.XVelService;
            Settings.Default.YAcc = machineSettings.YAcc;
            //Settings.Default.YLoad
            Settings.Default.YVelHigh = machineSettings.YVelHigh;
            Settings.Default.YVelLow = machineSettings.YVelLow;
            Settings.Default.YVelService = machineSettings.YVelService;
            Settings.Default.ZAcc = machineSettings.ZAcc;
            //Settings.Default.ZObjective
            Settings.Default.ZVelHigh = machineSettings.ZVelHigh;
            Settings.Default.ZVelLow = machineSettings.ZVelLow;
            Settings.Default.ZVelService = machineSettings.ZVelService;
            Settings.Default.XOffset = machineSettings.XOffset;
            Settings.Default.YOffset = machineSettings.YOffset;
            Settings.Default.XLoad = machineSettings.XLoad;
            Settings.Default.YLoad = machineSettings.YLoad;
            Settings.Default.ZeroFocusPoint = machineSettings.ZCamera;
            Settings.Default.ZeroPiercePoint = machineSettings.ZLaser;
            Settings.Default.XLeftPoint = machineSettings.XLeftPoint;
            Settings.Default.YLeftPoint = machineSettings.YLeftPoint;
            Settings.Default.XRightPoint = machineSettings.XRightPoint;
            Settings.Default.YRightPoint = machineSettings.YRightPoint;
        }

        internal static void CopyToSettings2(this MachineSettingsVM machineSettings, LaserMachineSettings laserMachineSettings)
        {
            var config = new MapperConfiguration(expr =>
            {
                expr.CreateMap<MachineSettingsVM, LaserMachineSettings>()
                .ForMember(dest => dest.ZeroFocusPoint, opt => opt.MapFrom(source => source.ZCamera))
                .ForMember(dest => dest.ZeroPiercePoint, opt => opt.MapFrom(source => source.ZLaser)); ;
            });
            var mapper = new Mapper(config);
            var obj = mapper.Map(machineSettings, laserMachineSettings);
        }
        internal static void CopyFromSettings(this MachineSettingsVM machineSettings)
        {
            machineSettings.XAcc = Settings.Default.XAcc;
            //Settings.Default.XLoad
            machineSettings.XVelHigh = Settings.Default.XVelHigh;
            machineSettings.XVelLow = Settings.Default.XVelLow;
            machineSettings.XVelService = Settings.Default.XVelService;
            machineSettings.YAcc = Settings.Default.YAcc;
            //Settings.Default.YLoad
            machineSettings.YVelHigh = Settings.Default.YVelHigh;
            machineSettings.YVelLow = Settings.Default.YVelLow;
            machineSettings.YVelService = Settings.Default.YVelService;
            machineSettings.ZAcc = Settings.Default.ZAcc;
            //Settings.Default.ZObjective
            machineSettings.ZVelHigh = Settings.Default.ZVelHigh;
            machineSettings.ZVelLow = Settings.Default.ZVelLow;
            machineSettings.ZVelService = Settings.Default.ZVelService;
            machineSettings.XOffset = Settings.Default.XOffset;
            machineSettings.YOffset = Settings.Default.YOffset;
            machineSettings.XLoad = Settings.Default.XLoad;
            machineSettings.YLoad = Settings.Default.YLoad;
            machineSettings.ZCamera = Settings.Default.ZeroFocusPoint;
            machineSettings.ZLaser = Settings.Default.ZeroPiercePoint;
            machineSettings.XLeftPoint = Settings.Default.XLeftPoint;
            machineSettings.YLeftPoint = Settings.Default.YLeftPoint;
            machineSettings.XRightPoint = Settings.Default.XRightPoint;
            machineSettings.YRightPoint = Settings.Default.YRightPoint;
        }
        internal static void CopyFromSettings2(this MachineSettingsVM machineSettings, LaserMachineSettings laserMachineSettings)
        {
            var config = new MapperConfiguration(expr =>
            {
                expr.CreateMap<LaserMachineSettings, MachineSettingsVM>()
                .ForMember(dest => dest.ZCamera, opt => opt.MapFrom(source => source.ZeroFocusPoint))
                .ForMember(dest => dest.ZLaser, opt => opt.MapFrom(source => source.ZeroPiercePoint));
            });
            var mapper = new Mapper(config);
            machineSettings = mapper.Map(laserMachineSettings, machineSettings);
        }
        internal static void GetOffsetByCurCoor(this IEnumerable<OffsetPoint> offsetPoints, double curX, double curY, ref double xOffset, ref double yOffset)
        {
            try
            {
                var sortResult = offsetPoints
                                .OrderBy(x => Math.Abs(x.X - curX))
                                .ThenBy(y => Math.Abs(y.Y - curY))
                                .First();
                if (sortResult != null)
                {
                    xOffset = sortResult.dx;
                    yOffset = sortResult.dy;
                }
            }
            catch (Exception) { }
        }
    }

}
