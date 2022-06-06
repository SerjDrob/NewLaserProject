namespace NewLaserProject.Classes.ProgBlocks
{
    /*internal*/public class TapperBlock : IProgBlock
    {
        public bool CanAcceptChildren { get; set; }        
        public double Tapper { get; set; }
    }
    //TODO add resetTapper block
}
