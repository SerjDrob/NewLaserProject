using MachineClassLibrary.BehaviourTree;
using MachineClassLibrary.Laser.Parameters;
using NewLaserProject.Classes.Process.Utility;
using NewLaserProject.Classes.ProgBlocks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

}