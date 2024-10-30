#define Snap

using System.Collections.Generic;
using System.Windows;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using NewLaserProject.Classes;

namespace NewLaserProject.ViewModels
{
    internal record WorkProcUnit(double WaferWidth, double WaferHeight, 
        Scale DefaultFileScale, bool WaferTurn90, 
        bool MirrorX, double WaferOffsetX, 
        double WaferOffsetY, double FileOffsetX,
        double FileOffsetY, List<(string, LaserEntity, int)> Objects, 
        string FileName, FileAlignment FileAlignment, 
        bool IsWaferMark, MarkPosition MarkPosition,
        Stack<(string[] layers, Rect selection)> ErasedObjects,
        Dictionary<string,bool> DisabledLayers);
}
