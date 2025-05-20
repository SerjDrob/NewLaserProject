using System.Collections;
using System.Collections.Generic;
using MachineClassLibrary.Machine.Machines;

internal class LaserMachineSettings : CommonMachineSettings
{
    public double? XLoad { get; set; }
    public double? YLoad { get; set; }
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
    public double? PazAngle { get; set; }
    public int? PWMBaudRate { get; set; }
    public bool? ScanheadInvertEntityAngle { get; set; }
    public List<OffsetPoint>? OffsetPoints { get; set; }
    public List<OffsetPoint>? CmdOffsetPoints { get; set; }

}

public record OffsetPoint(double X, double Y, double dx, double dy);
