using System.Collections.Generic;

namespace NewLaserProject.Classes.ProgBlocks
{
    internal class MainLoop : LoopBlock
    {
        public MainLoop()
        {

        }
        public MainLoop(int loopCount, bool shuffle, IEnumerable<IProgBlock> listing)
        {
            LoopCount = loopCount;
            Shuffle = shuffle;
            Children = new ChildrenObservCollection<IProgBlock>(this,listing);
        }
        public bool Shuffle { get; set; } = false;
    }

}
