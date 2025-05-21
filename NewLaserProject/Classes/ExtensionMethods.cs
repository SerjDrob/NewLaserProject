using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using AutoMapper;
using HandyControl.Data;
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
                if(offsetPoints.Count() < 2)//TODO check the zero count case
                {
                    xOffset = offsetPoints.Single().dx;
                    yOffset = offsetPoints.Single().dy;
                    return;
                } 
                var twoPointsX = offsetPoints.GetClosestPoints(curX, curY, true);
                var twoPointsY = offsetPoints.GetClosestPoints(curX, curY, false);


                if (twoPointsX.first == null || twoPointsX.second == null)
                {
                    xOffset = twoPointsX.first == null ? twoPointsX.second.dx : twoPointsX.first.dx;
                }
                else
                {
                    var points = GetSurroundPoints(offsetPoints, curX, curY, out var nw, out var ne, out var sw, out var se)
                        .Select(p=>(p.X,p.Y,p.dx))
                        .ToArray();

                    var result1 = InterpolateValue(curX, curY,points, 0,1,2);   
                    var result2 = InterpolateValue(curX, curY,points, 0,2,3);
                    xOffset = (result1 + result2) / 2;


                    //xOffset = curX.GetYLinear((twoPointsX.first.X, twoPointsX.first.dx), (twoPointsX.second.X, twoPointsX.second.dx));
                }
                
                if (twoPointsY.first == null || twoPointsY.second == null)
                {
                    yOffset = twoPointsY.first == null ? twoPointsY.second.dy : twoPointsY.first.dy;
                }
                else
                {
                    var points = GetSurroundPoints(offsetPoints, curX, curY, out var nw, out var ne, out var sw, out var se)
                       .Select(p => (p.Y, p.X, p.dy))
                       .ToArray();

                    var result1 = InterpolateValue(curY, curX, points, 0, 1, 2);
                    var result2 = InterpolateValue(curY, curX, points, 0, 2, 3);
                    yOffset = (result1 + result2) / 2;

                    //yOffset = curY.GetYLinear((twoPointsY.first.Y, twoPointsY.first.dy), (twoPointsY.second.Y, twoPointsY.second.dy));
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        static double LinearInterpolationInTriangle(double px, double py,
        (double x, double y) p1, (double x, double y) p2, (double x, double y) p3,
        double f1, double f2, double f3)
        {
            double detT = (p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y);
            double l1 = ((p2.y - p3.y) * (px - p3.x) + (p3.x - p2.x) * (py - p3.y)) / detT;
            double l2 = ((p3.y - p1.y) * (px - p3.x) + (p1.x - p3.x) * (py - p3.y)) / detT;
            double l3 = 1 - l1 - l2;

            return l1 * f1 + l2 * f2 + l3 * f3;
        }

        static bool PointInTriangle(double px, double py,
            (double x, double y) p1, (double x, double y) p2, (double x, double y) p3)
        {
            double Sign((double x, double y) p1, (double x, double y) p2, (double x, double y) p3)
            {
                return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
            }

            double d1 = Sign((px, py), p1, p2);
            double d2 = Sign((px, py), p2, p3);
            double d3 = Sign((px, py), p3, p1);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        static double InterpolateValue(double x, double y, (double x, double y, double f)[] points, int i1, int i2, int i3)
        {
            var (x0, y0, f00) = points[i1];
            var (x1, y1, f11) = points[i2];
            var (x2, y2, f22) = points[i3];

            if (PointInTriangle(x, y, (x0, y0), (x1, y1), (x2, y2)))
            {
                return LinearInterpolationInTriangle(x, y, (x0, y0), (x1, y1), (x2, y2), f00, f11, f22);
            }
            else
            {
                throw new ArgumentException("Точка не находится внутри заданных треугольников.");
            }
        }

        private static double GetYLinear(this double testX,
                                  (double x, double y) firstPoint,
                                  (double x, double y) secondPoint)
        {
            double x1 = firstPoint.x;
            double y1 = firstPoint.y;
            double x2 = secondPoint.x;
            double y2 = secondPoint.y;

            if (x1 == x2)
                throw new ArgumentException("Точки не должны иметь одинаковую координату X");
            //if (!(testX > x1 && testX < x2) || !(testX > x2 && testX < x1))
            //{
            //    var minX = x1 < x2 ? x1 : x2;
            //    var maxX = x2 < x1 ? x1 : x2;
            //    if (testX < minX) return minX;
            //    return maxX;
            //}

            double deltaX = x2 - x1;
            double deltaY = y2 - y1;

            return y1 + ((testX - x1) / deltaX) * deltaY;
        }

        private static double GetInterpolateFunc(double a1, double a2, double da1, double da2, double c)
        {
            if (a1 == a2) throw new ArgumentException("Точки не должны иметь одинаковое значение");
            return da1 + (c-a1)* (da2-da1)/(a2-a1);
        }


        private static (OffsetPoint first, OffsetPoint second) GetClosestPoints(
            this IEnumerable<OffsetPoint> offsetPoints,
            double x, double y, bool byX)
        {
            if (offsetPoints == null || !offsetPoints.Any() || offsetPoints.Count() < 2)
                return (null, null);
            OffsetPoint? nw, ne, sw, se;
            var arr = GetSurroundPoints(offsetPoints, x, y, out nw, out ne, out sw, out se);
            var notNullPoints = arr.Where(p => p != null).ToArray();
            if (notNullPoints.Count() == 1) return (notNullPoints.Single(), null);
            if (notNullPoints.Count() < 4) return (notNullPoints.First(), notNullPoints.Last());

            if (byX)
            {
                var avgNy = (nw.Y + ne.Y) / 2;
                var avgSy = (sw.Y + se.Y) / 2;

                return (Math.Abs(avgNy - y) < Math.Abs(avgSy - y)) ? (nw, ne) : (sw, se);
            }
            else
            {
                var avgWx = (nw.X + sw.X) / 2;
                var avgEx = (ne.X + se.X) / 2;

                return (Math.Abs(avgWx - x) < Math.Abs(avgEx - x)) ? (sw, nw) : (se, ne);
            }
        }

        private static OffsetPoint[] GetSurroundPoints(IEnumerable<OffsetPoint> offsetPoints, double x, double y, out OffsetPoint? nw, out OffsetPoint? ne, out OffsetPoint? sw, out OffsetPoint? se)
        {
            var getDistance = (OffsetPoint p1, (double x, double y) p2) =>
                            Math.Sqrt(Math.Pow(p1.X - p2.x, 2) + Math.Pow(p1.Y - p2.y, 2));

            nw = offsetPoints.Where(p => p.X < x && p.Y > y)
                 .MinBy(p => getDistance(p, (x, y)));
            ne = offsetPoints.Where(p => p.X > x && p.Y > y)
                 .MinBy(p => getDistance(p, (x, y)));
            sw = offsetPoints.Where(p => p.X < x && p.Y < y)
                 .MinBy(p => getDistance(p, (x, y)));
            se = offsetPoints.Where(p => p.X > x && p.Y < y)
                 .MinBy(p => getDistance(p, (x, y)));
            return [nw, ne, sw, se];
        }
    }

}
