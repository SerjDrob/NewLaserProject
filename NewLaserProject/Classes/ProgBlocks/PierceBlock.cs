using MachineClassLibrary.Laser.Parameters;

namespace NewLaserProject.Classes.ProgBlocks
{
    public class PierceBlock : IProgBlock
    {
        public bool CanAcceptChildren
        {
            get; set;
        }
        //public MarkLaserParams MarkParams { get; set; }
        public ExtendedParams MarkParams
        {
            get; set;
        }
    }

}
