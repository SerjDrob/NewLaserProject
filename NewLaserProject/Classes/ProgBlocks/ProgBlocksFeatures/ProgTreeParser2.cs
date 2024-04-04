using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using MachineClassLibrary.Laser.Parameters;

namespace NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures
{
    public static class ProgTreeParser2
    {
        public static ProcessingSequence GetProgBlocksSequence(string jsonTree)
        {
            var mainLoop = new JsonDeserializer<MainLoop>()
                .SetKnownType<AddZBlock>()
                .SetKnownType<DelayBlock>()
                .SetKnownType<LoopBlock>()
                .SetKnownType<PierceBlock>()
                .SetKnownType<TaperBlock>()
                .SetKnownType<RepairZBlock>()
                .SetKnownType<MarkLaserParams>()
                .SetKnownType<PenParams>()
                .SetKnownType<HatchParams>()
                .SetKnownType<ExtendedParams>()
                .Deserialize(jsonTree);

            var seq = Enumerable.Repeat(Parse(mainLoop.Children), mainLoop.LoopCount).SelectMany(x => x);
            
            return new ProcessingSequence(seq, mainLoop.LoopCount, mainLoop.Shuffle);

            IEnumerable<IProgBlock> Parse(IEnumerable<IProgBlock> progBlocks)
            {
                var blocks = Enumerable.Empty<IProgBlock>();
                foreach (var progBlock in progBlocks)
                {
                    if (progBlock is LoopBlock loop)
                    {
                        var loopBlock = Enumerable.Repeat(Parse(loop.Children), loop.LoopCount)
                            .SelectMany(x => x);
                        blocks = blocks.Concat(loopBlock);
                    }
                    else
                    {
                        blocks = blocks.Append(progBlock);
                    }
                }
                return blocks;
            }
        }
    }

}
