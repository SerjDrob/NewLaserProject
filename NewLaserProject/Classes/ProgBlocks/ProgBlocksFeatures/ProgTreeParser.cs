using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using MachineClassLibrary.BehaviourTree;
using MachineClassLibrary.Laser.Parameters;
using NewLaserProject.Classes.Process.Utility;

namespace NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures
{
    public class ProgTreeParser
    {
        private readonly MainLoop _progModules;
        public bool MainLoopShuffle => _progModules.Shuffle;
        public int MainLoopCount => _progModules.LoopCount;

        private readonly Dictionary<Type, object> _functions = new();
        private readonly CancellationToken _cancellationToken;

        public ProgTreeParser SetModuleFunction<TBlock, T>(IFuncProxy<T> funcProxy) where TBlock : IProgBlock
        {
            _functions.TryAdd(typeof(TBlock), funcProxy);
            return this;
        }
        public ProgTreeParser(string jsonTree, CancellationToken cancellationToken = new())
        {
            _progModules = new JsonDeserializer<MainLoop>()
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
            _cancellationToken = cancellationToken;
        }
        public FuncTree GetTree()
        {
            var result = ParseModules(_progModules.Children);
            return result;
        }
        private FuncTree ParseModules(IEnumerable<IProgBlock> progModules)
        {
            var mainLoop = FuncTree.StartLoop(1, _cancellationToken);
            foreach (var item in progModules)
            {
                if (item is LoopBlock loop)
                {
                    var child = ParseModules(loop.Children);
                    var endLoop = FuncTree.StartLoop(loop.LoopCount, _cancellationToken)
                        .AddChild(child)
                        .EndLoop;
                    mainLoop.AddChild(endLoop);
                    continue;
                }

                var fp = _functions[item.GetType()];
                var function = item switch
                {
                    TaperBlock tapperBlock => ((IFuncProxy<double>)fp).GetFuncWithArgument(tapperBlock.Tapper),
                    AddZBlock addZBlock => ((IFuncProxy<double>)fp).GetFuncWithArgument(addZBlock.DeltaZ),
                    DelayBlock delayBlock => ((IFuncProxy<int>)fp).GetFuncWithArgument(delayBlock.DelayTime),
                    PierceBlock pierceBlock => ((IFuncProxy<ExtendedParams>)fp).GetFuncWithArgument(pierceBlock.MarkParams),
                    _ => throw new ArgumentException($"Unknown type {nameof(item)}")
                };
                mainLoop.AddChild(FuncTree.SetFunc(function));
            }
            return mainLoop.EndLoop;
        }
    }



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
