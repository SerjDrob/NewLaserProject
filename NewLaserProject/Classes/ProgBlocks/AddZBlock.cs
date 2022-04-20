namespace NewLaserProject.Classes.ProgBlocks
{
    /*internal*/public class AddZBlock : IProgBlock
    {
        public bool CanAcceptChildren { get; set; }       
        public double DeltaZ { get; set; }        
    }

}
