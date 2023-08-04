using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MachineClassLibrary.Laser.Entities;

namespace NewLaserProject.Views;
internal class ProcObjTabView
{
    public int Index
    {
        get;
        set;
    }
    public IProcObject ProcObject
    {
        get;
        set;
    }
}
