using MachineClassLibrary.BehaviourTree;
using MachineClassLibrary.Laser.Parameters;
using NewLaserProject;
using NewLaserProject.Classes;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public class ProgTreeParser
    {
        private readonly MainLoop _progModules;
        public bool MainLoopShuffle { get => _progModules.Shuffle; }
        public int MainLoopCount { get => _progModules.LoopCount; }
        
        private Dictionary<Type, object> _functions = new();

        public ProgTreeParser SetModuleFunction<TBlock, T>(IFuncProxy<T> funcProxy) where TBlock : IProgBlock
        {
            _functions.TryAdd(typeof(TBlock), funcProxy);
            return this;
        }
        public ProgTreeParser(string jsonTree)
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
        }

        public FuncTree GetTree()
        {
            var result = ParseModules(_progModules.Children);
            return result;
        }


        private FuncTree ParseModules(IEnumerable<IProgBlock> progModules)
        {
            var mainLoop = FuncTree.StartLoop(1);
            foreach (var item in progModules)
            {
                if (item is LoopBlock loop)
                {
                    FuncTree child = ParseModules(loop.Children);
                    FuncTree endLoop = FuncTree.StartLoop(loop.LoopCount)
                        .AddChild(child)
                        .EndLoop;
                    mainLoop.AddChild(endLoop);
                    continue;
                }

                var fp = _functions[item.GetType()];
                Func<Task> function = item switch
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