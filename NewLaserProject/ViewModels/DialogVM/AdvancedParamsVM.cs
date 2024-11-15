using MachineControlsLibrary.CommonDialog;

namespace NewLaserProject.ViewModels.DialogVM
{
    public class AdvancedParamsVM : CommonDialogResultable<AdvancedParamsVM>
    {
        public bool XInvertDirSignal { get; set; }
        public bool XInvertAxesDirection { get; set; }
        public bool XInvertEncoder { get; set; }
        public int XPPU { get; set; }//= 2000;
        public int XDenominator { get; set; }
        public bool YInvertDirSignal { get; set; }
        public bool YInvertAxesDirection { get; set; }
        public bool YInvertEncoder { get; set; }
        public int YPPU { get; set; }
        public int YDenominator { get; set; }
        public bool ZInvertDirSignal { get; set; }
        public bool ZInvertAxesDirection { get; set; }
        public bool ZInvertEncoder { get; set; }
        public int ZPPU { get; set; }
        public int ZDenominator { get; set; }
        public bool UInvertDirSignal { get; set; }
        public bool UInvertAxesDirection { get; set; }
        public bool UInvertEncoder { get; set; }
        public int UPPU { get; set; }
        public int UDenominator { get; set; }
        public bool VideoMirrorX { get; set; }
        public bool VideoMirrorY { get; set; }
        public int PWMBaudRate { get; set; }
        public bool ScanheadInvertEntityAngle { get; set; }

        public override void SetResult() => SetResult(this);

    }
}
