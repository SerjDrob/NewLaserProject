namespace NewLaserProject.Classes.ProgBlocks
{
    internal class AddZBlock : IProgBlock
    {
        public bool CanAcceptChildren { get; set; }       
        public double DeltaZ { get; set; }        
    }

}
