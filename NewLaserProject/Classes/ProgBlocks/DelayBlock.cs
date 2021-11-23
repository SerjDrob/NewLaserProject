namespace NewLaserProject.Classes.ProgBlocks
{
    internal class DelayBlock : IProgBlock
    {
        public bool CanAcceptChildren { get; set; }       
        public int DelayTime { get; set; }        
    }

}
