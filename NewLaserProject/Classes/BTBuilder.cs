using MachineClassLibrary.BehaviourTree;
using MachineClassLibrary.Laser;
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

    //public class BTBuilderX
    //{
    //    private readonly List<IProgBlock> _progModules;
    //    public BTBuilderX(string jsonTree)
    //    {
    //        try
    //        {
    //            _progModules = JsonConvert.DeserializeObject<List<IProgBlock>>(jsonTree, new JsonSerializerSettings
    //            {
    //                TypeNameHandling = TypeNameHandling.Objects,
    //                SerializationBinder = new TypesBinder
    //                {
    //                    KnownTypes = new List<Type>
    //                {
    //                    typeof(AddZBlock),
    //                    typeof(DelayBlock),
    //                    typeof(LoopBlock),
    //                    typeof(PierceBlock),
    //                    typeof(TapperBlock),
    //                    typeof(MarkLaserParams),
    //                    typeof(PenParams),
    //                    typeof(HatchParams)
    //                }
    //                }
    //            }) ?? throw new ArgumentException($"Can not deserialize {nameof(jsonTree)}");
    //        }
    //        catch (Exception ex)
    //        {

    //            throw;
    //        }
    //    }
    //    public BTBuilderX SetModuleAction(Type type, IFuncProxy funcProxy)
    //    {
    //        _actions.TryAdd(type, funcProxy);
    //        return this;
    //    }
    //    private Dictionary<Type, IFuncProxy> _actions = new();


    //    public Sequence GetSequence()
    //    {
    //        var result = ParseModules(_progModules);
    //        return result;
    //    }
    //    private Sequence ParseModules(IEnumerable<IProgBlock> progModules)
    //    {
    //        var sequence = new Sequence();
    //        foreach (var item in progModules)
    //        {
    //            if (item.GetType() != typeof(LoopBlock))
    //            {
    //                IFuncProxy fp;
    //                if (_actions.TryGetValue(item.GetType(), out fp))
    //                {
    //                    var action = item switch
    //                    {
    //                        TapperBlock tapperBlock => fp.GetActionWithArguments(tapperBlock.Tapper),
    //                        AddZBlock addZBlock => fp.GetActionWithArguments(addZBlock.DeltaZ),
    //                        DelayBlock delayBlock => fp.GetActionWithArguments(delayBlock.DelayTime),
    //                        PierceBlock pierceBlock => fp.GetActionWithArguments(pierceBlock.MarkParams),
    //                        _ => throw new ArgumentException($"Unknown type {nameof(item)}")
    //                    };
    //                    sequence.Hire(new Leaf(() => Task.Run(action)));
    //                }
    //                else
    //                {
    //                    throw new KeyNotFoundException($"There is no value for {item.GetType()} key");
    //                }
    //            }
    //            else if (item.GetType() == typeof(LoopBlock))
    //            {
    //                sequence.Hire(new Ticker(((LoopBlock)item).LoopCount).Hire(ParseModules(((LoopBlock)item).Children)));
    //            }
    //        }
    //        return sequence;
    //        // var f = new FuncProxy<Action<int>>(x => { });
    //    }
    //}
    //public class BTBuilderY
    //{
    //    private readonly MainLoop _progModules;
    //    private Dictionary<Type, IFuncProxy> _actions = new();
    //    public bool MainLoopShuffle { get => _progModules.Shuffle; }
    //    public int MainLoopCount { get => _progModules.LoopCount; }

    //    public BTBuilderY(string jsonTree)
    //    {

    //        _progModules = JsonConvert.DeserializeObject<MainLoop>(jsonTree, new JsonSerializerSettings
    //        {
    //            TypeNameHandling = TypeNameHandling.Objects,
    //            SerializationBinder = new TypesBinder
    //            {
    //                KnownTypes = new List<Type>
    //                {
    //                    typeof(AddZBlock),
    //                    typeof(DelayBlock),
    //                    typeof(LoopBlock),
    //                    typeof(PierceBlock),
    //                    typeof(TapperBlock),                        
    //                    typeof(RepairZBlock),
    //                    typeof(MarkLaserParams),
    //                    typeof(PenParams),
    //                    typeof(HatchParams),
    //                    typeof(MainLoop)
    //                }
    //            }
    //        }) ?? throw new ArgumentException($"Can not deserialize {nameof(jsonTree)}");
    //    }

    //    public BTBuilderY SetModuleAction(Type type, IFuncProxy funcProxy)
    //    {
    //        _actions.TryAdd(type, funcProxy);
    //        return this;
    //    }

    //    public BTBuilderY SetModuleAction<TBlock>(IFuncProxy funcProxy) where TBlock : IProgBlock
    //    {
    //        _actions.TryAdd(typeof(TBlock), funcProxy);
    //        return this;
    //    }

    //    public ActionTree GetTree()
    //    {
    //        var result = ParseModules(_progModules.Children);
    //        return result;
    //    }


    //    private ActionTree ParseModules(IEnumerable<IProgBlock> progModules)
    //    {
    //        var mainLoop = ActionTree.StartLoop(1);
    //        foreach (var item in progModules)
    //        {
    //            if (item.GetType() != typeof(LoopBlock))
    //            {
    //                IFuncProxy fp;
    //                if (_actions.TryGetValue(item.GetType(), out fp))
    //                {
    //                    var action = item switch
    //                    {
    //                        TapperBlock tapperBlock => fp.GetActionWithArguments(tapperBlock.Tapper),
    //                        AddZBlock addZBlock => fp.GetActionWithArguments(addZBlock.DeltaZ),
    //                        DelayBlock delayBlock => fp.GetActionWithArguments(delayBlock.DelayTime),
    //                        PierceBlock pierceBlock => fp.GetActionWithArguments(pierceBlock.MarkParams),
    //                        _ => throw new ArgumentException($"Unknown type {nameof(item)}")
    //                    };
    //                    mainLoop.AddChild(ActionTree.SetAction(action));
    //                }
    //                else
    //                {
    //                    throw new KeyNotFoundException($"There is no value for {item.GetType()} key");
    //                }
    //            }
    //            else if (item.GetType() == typeof(LoopBlock))
    //            {
    //                var loop = (LoopBlock)item;
    //                mainLoop.AddChild(ActionTree.StartLoop(loop.LoopCount).AddChild(ParseModules(loop.Children)).EndLoop);
    //            }
    //        }
    //        return mainLoop.EndLoop;
    //    }
    //}

    //public class BTBuilderZ
    //{
    //    private readonly MainLoop _progModules;
    //    private Dictionary<Type, dynamic> _functions = new();
    //    public bool MainLoopShuffle { get => _progModules.Shuffle; }
    //    public int MainLoopCount { get => _progModules.LoopCount; }

    //    public BTBuilderZ(string jsonTree)
    //    {

    //        _progModules = JsonConvert.DeserializeObject<MainLoop>(jsonTree, new JsonSerializerSettings
    //        {
    //            TypeNameHandling = TypeNameHandling.Objects,
    //            SerializationBinder = new TypesBinder
    //            {
    //                KnownTypes = new List<Type>
    //                {
    //                    typeof(AddZBlock),
    //                    typeof(DelayBlock),
    //                    typeof(LoopBlock),
    //                    typeof(PierceBlock),
    //                    typeof(TapperBlock),
    //                    typeof(RepairZBlock),
    //                    typeof(MarkLaserParams),
    //                    typeof(PenParams),
    //                    typeof(HatchParams),
    //                    typeof(MainLoop)
    //                }
    //            }
    //        }) ?? throw new ArgumentException($"Can not deserialize {nameof(jsonTree)}");
    //    }

    //    public BTBuilderZ SetModuleFunction<TBlock,T>(IFuncProxy2<T> funcProxy) where TBlock : IProgBlock
    //    {
    //        _functions.TryAdd(typeof(TBlock), funcProxy);
    //        return this;
    //    }

    //    public FuncTree GetTree()
    //    {
    //        var result = ParseModules(_progModules.Children);
    //        return result;
    //    }


    //    private FuncTree ParseModules(IEnumerable<IProgBlock> progModules)
    //    {
    //        var mainLoop = FuncTree.StartLoop(1);
    //        foreach (var item in progModules)
    //        {
    //            if (item is LoopBlock loop)
    //            {
    //                FuncTree child = ParseModules(loop.Children);
    //                FuncTree endLoop = FuncTree.StartLoop(loop.LoopCount).AddChild(child).EndLoop;
    //                mainLoop.AddChild(endLoop);
    //                continue;
    //            }

    //            var fp = _functions[item.GetType()];
    //            var function = item switch
    //            {
    //                TapperBlock tapperBlock => fp.GetFuncWithArguments(tapperBlock.Tapper),
    //                AddZBlock addZBlock => fp.GetFuncWithArguments(addZBlock.DeltaZ),
    //                DelayBlock delayBlock => fp.GetFuncWithArguments(delayBlock.DelayTime),
    //                PierceBlock pierceBlock => fp.GetFuncWithArguments(pierceBlock.MarkParams),
    //                _ => throw new ArgumentException($"Unknown type {nameof(item)}")
    //            };
    //            mainLoop.AddChild(FuncTree.SetFunc(function));
    //        }
    //        return mainLoop.EndLoop;
    //    }
    //}

    public class ProgTreeParser
    {
        private readonly MainLoop _progModules;
        public bool MainLoopShuffle { get => _progModules.Shuffle; }
        public int MainLoopCount { get => _progModules.LoopCount; }
        
        private Dictionary<Type, object> _functions = new();

        public ProgTreeParser SetModuleFunction<TBlock, T>(IFuncProxy2<T> funcProxy) where TBlock : IProgBlock
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
                .SetKnownType<TapperBlock>()
                .SetKnownType<RepairZBlock>()
                .SetKnownType<MarkLaserParams>()
                .SetKnownType<PenParams>()
                .SetKnownType<HatchParams>()
                .SetKnownType<MainLoop>()
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
                    FuncTree endLoop = FuncTree.StartLoop(loop.LoopCount).AddChild(child).EndLoop;
                    mainLoop.AddChild(endLoop);
                    continue;
                }

                var fp = _functions[item.GetType()];
                Func<Task> function = item switch
                {
                    TapperBlock tapperBlock => ((IFuncProxy2<double>)fp).GetFuncWithArgument(tapperBlock.Tapper),
                    AddZBlock addZBlock => ((IFuncProxy2<double>)fp).GetFuncWithArgument(addZBlock.DeltaZ),
                    DelayBlock delayBlock => ((IFuncProxy2<int>)fp).GetFuncWithArgument(delayBlock.DelayTime),
                    PierceBlock pierceBlock => ((IFuncProxy2<ExtendedParams>)fp).GetFuncWithArgument(pierceBlock.MarkParams),
                    _ => throw new ArgumentException($"Unknown type {nameof(item)}")
                };
                mainLoop.AddChild(FuncTree.SetFunc(function));
            }
            return mainLoop.EndLoop;
        }
    }
        
}