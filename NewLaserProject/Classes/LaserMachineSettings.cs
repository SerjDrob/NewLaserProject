using System.Collections;
using System.Collections.Generic;

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
    public double? XVelService { get; set; }
    public double? YVelService { get; set; }
    public double? ZVelService { get; set; }
    public double? YVelLow { get; set; }
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
    public double? WaferWidth { get; set; }
    public double? WaferHeight { get; set; }
    public double? WaferThickness { get; set; }
    public int? PreferredCameraCapabilities { get; set; }
    public double? PazAngle { get; set; }
    public double? XHomeVelLow { get; set; }
    public double? YHomeVelLow { get; set; }
    public double? ZHomeVelLow { get; set; }
    public List<OffsetPoint>? OffsetPoints { get; set; }

}

public record OffsetPoint(double X, double Y, double dx, double dy);
