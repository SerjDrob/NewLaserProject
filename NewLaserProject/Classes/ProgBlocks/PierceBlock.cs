using MachineClassLibrary.Laser.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Classes.ProgBlocks
{
    /*internal*/
    public class PierceBlock : IProgBlock
    {        
        public bool CanAcceptChildren { get; set; }
        //public MarkLaserParams MarkParams { get; set; }
        public ExtendedParams MarkParams { get; set; }
    }

}
