using System.Collections.Generic;
using NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures;

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
            Children = new ChildrenObservableCollection<IProgBlock>(this,listing);
        }
        public string ProgName
        {
            get;
            set;
        }
        public string MaterialName
        {
            get;
            set;
        }
        public bool Shuffle { get; set; } = false;
    }

}
