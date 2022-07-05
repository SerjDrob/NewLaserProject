using System.Threading.Tasks;
using System;

namespace NewLaserProject.Classes.ProgBlocks
{
    /*internal*/public class TaperBlock : IProgBlock
    {        
        public bool CanAcceptChildren { get; set; }        
        public double Tapper { get; set; }        
    }
    //TODO add resetTapper block
}
