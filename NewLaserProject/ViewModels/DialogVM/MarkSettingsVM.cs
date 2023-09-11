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
        [Browsable(false)]
        public int StartTC { get; set; } = 0;
        [Category("Параметры пера")]
        [Browsable(false)]
        public int LaserOnTC { get; set; } = 0;
        [Category("Параметры пера")]
        [Browsable(false)]
        public int LaserOffTC { get; set; } = 0;
        [Category("Параметры пера")]
        [Browsable(false)]
        public int EndTC { get; set; } = 0;
        [Category("Параметры пера")]
        [Browsable(false)]
        public int PolyTC { get; set; } = 0;
        [Category("Параметры пера")]
        [Browsable(false)]
        public double JumpSpeed { get; set; } = 1000;
        [Category("Параметры пера")]
        [Browsable(false)]
        public int JumpPosTC { get; set; } = 1000;
        [Category("Параметры пера")]
        [Browsable(false)]
        public int JumpDistTC { get; set; } = 1000;
        [Category("Параметры пера")]
        [Browsable(false)]
        public double EndComp { get; set; } = 0;
        [Category("Параметры пера")]
        [Browsable(false)]
        public double AccDist { get; set; } = 1;
        [Category("Параметры пера")]
        [Browsable(false)]
        public double PointTime { get; set; } = 1;
        [Category("Параметры пера")]
        [Browsable(false)]
        public bool PulsePointMode { get; set; } = false;
        [Category("Параметры пера")]
        [Browsable(false)]
        int PulseNum { get; set; } = 2;
        [Category("Параметры пера")]
        [DisplayName("Скорость переходов")]
        [Browsable(false)]
        public double FlySpeed { get; set; } = 1000;
        #endregion


        #region Hatch Params
        [Category("Параметры штриховки")]
        [DisplayName("Проходить контур")]
        public bool EnableContour { get; set; } = true;
        [Category("Параметры штриховки")]
        [Browsable(false)]
        public int ParamIndex { get; set; } = 1;
        [Category("Параметры штриховки")]
        [DisplayName("Штриховать")]
        public bool EnableHatch { get; set; } = true;
        [Category("Параметры штриховки")]
        [DisplayName("Перо штриховки")]
        public int HatchPenNo { get; set; } = 0;
        [Category("Параметры штриховки")]
        [DisplayName("Тип штриховки")]
        public int HatchType { get; set; } = 3;
        [Category("Параметры штриховки")]
        [Browsable(false)]
        public bool HatchAllCalc { get; set; } = false;
        [Category("Параметры штриховки")]
        [DisplayName("Штриховать край")]
        public bool HatchEdge { get; set; } = true;
        [Category("Параметры штриховки")]
        [DisplayName("Штриховать среднюю линию")]
        public bool HatchAverageLine { get; set; } = true;
        [Category("Параметры штриховки")]
        [DisplayName("Шаг штриховки, мм")]
        public double HatchLineDist { get; set; } = 0.05;
        [Category("Параметры штриховки")]
        [DisplayName("Отступ от края, мм")]
        public double HatchEdgeDist { get; set; } = 0.015;
        [Category("Параметры штриховки")]
        [DisplayName("Смещение начала штриховки, мм")]
        public double HatchStartOffset { get; set; } = 0;
        [Category("Параметры штриховки")]
        [DisplayName("Смещение конца штриховки, мм")]
        public double HatchEndOffset { get; set; } = 0;
        [Category("Параметры штриховки")]
        [Browsable(false)]
        public double HatchLineReduction { get; set; } = 0.01;
        [Category("Параметры штриховки")]
        [Browsable(false)]
        public double HatchLoopDist { get; set; } = 1;
        [Category("Параметры штриховки")]
        [DisplayName("Проходов по контуру")]
        public int EdgeLoop { get; set; } = 3;
        [Category("Параметры штриховки")]
        [DisplayName("Реверсивная штриховка")]
        public bool HatchLoopRev { get; set; } = true;
        [Category("Параметры штриховки")]
        [Browsable(false)]
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
