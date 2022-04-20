namespace NewLaserProject.Classes.ProgBlocks
{
    /*internal*/public class DelayBlock : IProgBlock
    {
        public bool CanAcceptChildren { get; set; }       
        public int DelayTime { get; set; }        
    }

}
