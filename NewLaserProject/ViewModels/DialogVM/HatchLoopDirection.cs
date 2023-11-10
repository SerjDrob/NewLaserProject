using MachineClassLibrary.Laser;

namespace NewLaserProject.ViewModels.DialogVM
{
    public enum HatchLoopDirection
    {
        Hatch_IN = JczLmc.HATCHATTRIB_LOOP,
        Hatch_OUT = JczLmc.HATCHATTRIB_LOOP | JczLmc.HATCHATTRIB_OUT
    }
}
