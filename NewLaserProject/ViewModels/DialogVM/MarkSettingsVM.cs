using System.ComponentModel;
using System.Windows.Input;
using MachineClassLibrary.Laser.Parameters;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace NewLaserProject.ViewModels.DialogVM
{

    [INotifyPropertyChanged]
    public partial class MarkSettingsVM : CommonDialogResultable<MarkSettingsVM>
    {
        #region Pen params
        [Category("Параметры пера")]
        [DisplayName("Номер пера")]
        public int PenNo { get; set; } = 0;
        [Category("Параметры пера")]
        [DisplayName("Количество проходов")]
        public int MarkLoop { get; set; } = 7;
        [Category("Параметры пера")]
        [DisplayName("Скорость маркировки")]
        public double MarkSpeed { get; set; } = 53;
        [Category("Параметры пера")]
        [DisplayName("Мощность")]
        public double PowerRatio { get; set; } = 100;
        [Category("Параметры пера")]
        [DisplayName("Ток")]
        public double Current { get; set; } = 3;
        [Category("Параметры пера")]
        [DisplayName("Частота")]
        public int Freq { get; set; } = 30000;
        [Category("Параметры пера")]
        [DisplayName("Ширина импульса")]
        public int QPulseWidth { get; set; } = 15;

        [Category("Параметры пера")]
        [DisplayName("Модулировать частоту")]
        public bool IsModulated { get; set; } = false;

        [Category("Параметры пера")]
        [DisplayName("Частота модуляции, Гц")]
        public int ModFreq { get; set; } = 300;

        [Category("Параметры пера")]
        [DisplayName("Скважность модуляции, %")]
        public int ModDutyCycle { get; set; } = 80;


        [Category("Параметры пера")]
        public int StartTC { get; set; } = 0;
        [Category("Параметры пера")]
        public int LaserOnTC { get; set; } = 0;
        [Category("Параметры пера")]
        public int LaserOffTC { get; set; } = 0;
        [Category("Параметры пера")]
        public int EndTC { get; set; } = 0;
        [Category("Параметры пера")]
        public int PolyTC { get; set; } = 0;
        [Category("Параметры пера")]
        public double JumpSpeed { get; set; } = 1000;
        [Category("Параметры пера")]
        public int JumpPosTC { get; set; } = 1000;
        [Category("Параметры пера")]
        public int JumpDistTC { get; set; } = 1000;
        [Category("Параметры пера")]
        public double EndComp { get; set; } = 0;
        [Category("Параметры пера")]
        public double AccDist { get; set; } = 1;
        [Category("Параметры пера")]
        public double PointTime { get; set; } = 1;
        [Category("Параметры пера")]
        public bool PulsePointMode { get; set; } = true;
        [Category("Параметры пера")]
        int PulseNum { get; set; } = 2;
        [Category("Параметры пера")]
        [DisplayName("Скорость переходов")]
        public double FlySpeed { get; set; } = 1000;
        #endregion


        #region Hatch Params
        [Category("Параметры штриховки")]
        [DisplayName("Маркировать контур")]
        public bool EnableContour { get; set; } = true;
        [Category("Параметры штриховки")]
        public int ParamIndex { get; set; } = 1;
        [Category("Параметры штриховки")]
        public bool EnableHatch { get; set; } = true;
        [Category("Параметры штриховки")]
        [DisplayName("Перо штриховки")]
        public int HatchPenNo { get; set; } = 0;
        [Category("Параметры штриховки")]
        [DisplayName("Тип штриховки")]
        public int HatchType { get; set; } = 3;
        [Category("Параметры штриховки")]
        public bool HatchAllCalc { get; set; } = false;
        [Category("Параметры штриховки")]
        [DisplayName("Штриховать край")]
        public bool HatchEdge { get; set; } = true;
        [Category("Параметры штриховки")]
        [DisplayName("Штриховать среднюю линию")]
        public bool HatchAverageLine { get; set; } = true;
        [Category("Параметры штриховки")]
        [DisplayName("Шаг штриховки")]
        public double HatchLineDist { get; set; } = 0.05;
        [Category("Параметры штриховки")]
        [DisplayName("Отступ от края")]
        public double HatchEdgeDist { get; set; } = 0.015;
        [Category("Параметры штриховки")]
        [DisplayName("Смещение начала штриховки")]
        public double HatchStartOffset { get; set; } = 0;
        [Category("Параметры штриховки")]
        [DisplayName("Смещение конца штриховки")]
        public double HatchEndOffset { get; set; } = 0;
        [Category("Параметры штриховки")]
        public double HatchLineReduction { get; set; } = 0.01;
        [Category("Параметры штриховки")]
        public double HatchLoopDist { get; set; } = 1;
        [Category("Параметры штриховки")]
        public int EdgeLoop { get; set; } = 3;
        [Category("Параметры штриховки")]
        public bool HatchLoopRev { get; set; } = true;
        [Category("Параметры штриховки")]
        public bool HatchAutoRotate { get; set; } = false;
        [Category("Параметры штриховки")]
        [DisplayName("Угол штриховки")]
        public double HatchRotateAngle { get; set; } = 0;


        #endregion
        
        public MarkLaserParams GetLaserParams()
        {
            var pen = new PenParams(PenNo, MarkLoop, MarkSpeed, PowerRatio, Current, Freq, QPulseWidth, IsModulated, ModFreq,
                                    ModDutyCycle, StartTC, LaserOnTC, LaserOffTC, EndTC, PolyTC, JumpSpeed, JumpPosTC, JumpDistTC,
                                    EndComp, AccDist, PointTime, PulsePointMode, PulseNum, FlySpeed);

            var hatch = new HatchParams(EnableContour, ParamIndex, EnableHatch, PenNo, HatchType, HatchAllCalc,
                                        HatchEdge, HatchAverageLine, HatchLineDist, HatchEdgeDist, HatchStartOffset, HatchEndOffset,
                                        HatchLineReduction, HatchLoopDist, EdgeLoop, HatchLoopRev, HatchAutoRotate, HatchRotateAngle);

            return new MarkLaserParams(pen, hatch);
        }

        public override void SetResult() => SetResult(this);
    }


}
