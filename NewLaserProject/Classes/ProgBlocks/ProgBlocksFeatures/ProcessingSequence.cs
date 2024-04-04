using System.Collections;
using System.Collections.Generic;

namespace NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures
{
    public class ProcessingSequence : IEnumerable<IProgBlock>
    {
        private readonly IEnumerable<IProgBlock> _progBlocks;

        public ProcessingSequence(IEnumerable<IProgBlock> progBlocks, int mainLoopCount, bool mainLoopShuffle)
        {
            _progBlocks = progBlocks;
            MainLoopCount = mainLoopCount;
            MainLoopShuffle = mainLoopShuffle;
        }

        public int MainLoopCount { get; init; }
        public bool MainLoopShuffle { get; init; }

        public IEnumerator<IProgBlock> GetEnumerator()
        {
            return _progBlocks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
