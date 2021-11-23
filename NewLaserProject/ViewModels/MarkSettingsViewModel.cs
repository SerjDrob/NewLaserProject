using MachineClassLibrary.Laser;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class MarkSettingsViewModel
    {
        #region Pen params
        public int PenNo { get; set; } = 0;
        public int MarkLoop { get; set; } = 7;
        public double MarkSpeed { get; set; } = 53;
        public double PowerRatio { get; set; } = 100;
        public double Current { get; set; } = 3;
        public int Freq { get; set; } = 30000;
        public int QPulseWidth { get; set; } = 15;
        public int StartTC { get; set; } = 0;
        public int LaserOnTC { get; set; } = 0;
        public int LaserOffTC { get; set; } = 0;
        public int EndTC { get; set; } = 0;
        public int PolyTC { get; set; } = 0;
        public double JumpSpeed { get; set; } = 1000;
        public int JumpPosTC { get; set; } = 1000;
        public int JumpDistTC { get; set; } = 1000;
        public double EndComp { get; set; } = 0;
        public double AccDist { get; set; } = 1;
        public double PointTime { get; set; } = 1;
        public bool PulsePointMode { get; set; } = true;
        int PulseNum { get; set; } = 2;
        public double FlySpeed { get; set; } = 1000;
        #endregion


        #region Hatch Params
        public bool EnableContour { get; set; } = true;
        public int ParamIndex { get; set; } = 1;
        public int EnableHatch { get; set; } = 13;
        public int HatchPenNo { get; set; } = 0;
        public int HatchType { get; set; } = 3;
        public bool HatchAllCalc { get; set; } = false;
        public bool HatchEdge { get; set; } = true;
        public bool HatchAverageLine { get; set; } = true;
        public double HatchLineDist { get; set; } = 0.05;
        public double HatchEdgeDist { get; set; } = 0.015;
        public double HatchStartOffset { get; set; } = 0;
        public double HatchEndOffset { get; set; } = 0;
        public double HatchLineReduction { get; set; } = 0.01;
        public double HatchLoopDist { get; set; } = 1;
        public int EdgeLoop { get; set; } = 3;
        public bool HatchLoopRev { get; set; } = true;
        public bool HatchAutoRotate { get; set; } = false;
        public double HatchRotateAngle { get; set; } = 0;
        #endregion

        public MarkLaserParams GetLaserParams()
        {
            var pen = new PenParams(PenNo, MarkLoop, MarkSpeed, PowerRatio, Current, Freq, QPulseWidth, StartTC,
                                    LaserOnTC, LaserOffTC, EndTC, PolyTC, JumpSpeed, JumpPosTC, JumpDistTC, EndComp, 
                                    AccDist, PointTime,PulsePointMode, PulseNum, FlySpeed);
            
            var hatch = new HatchParams(EnableContour, ParamIndex, EnableHatch, PenNo, HatchType, HatchAllCalc, 
                                        HatchEdge, HatchAverageLine, HatchLineDist, HatchEdgeDist,HatchStartOffset, HatchEndOffset, 
                                        HatchLineReduction, HatchLoopDist, EdgeLoop, HatchLoopRev, HatchAutoRotate, HatchRotateAngle);
            
            return new MarkLaserParams(pen,hatch);
        }

    }


}
