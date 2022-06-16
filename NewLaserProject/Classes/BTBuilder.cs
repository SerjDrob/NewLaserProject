﻿using MachineClassLibrary.BehaviourTree;
using MachineClassLibrary.Laser;
using NewLaserProject;
using NewLaserProject.Classes;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{

    public class BTBuilder
    {
        private readonly List<ProgModuleItemVM> _progModules;
        public BTBuilder(string jsonTree)
        {
            _progModules = JsonConvert.DeserializeObject<List<ProgModuleItemVM>>(jsonTree);
        }
        public BTBuilder SetModuleAction(ModuleType moduleType, IFuncProxy funcProxy)
        {
            _actions.TryAdd(moduleType, funcProxy);
            return this;
        }
        private Dictionary<ModuleType, IFuncProxy> _actions = new();


        public Sequence GetSequence()
        {
            var result = ParseModules(_progModules);
            return result;
        }
        private Sequence ParseModules(IEnumerable<ProgModuleItemVM> progModules)
        {
            var sequence = new Sequence();
            foreach (var item in progModules)
            {
                if (item.ModuleType != ModuleType.Loop)
                {
                    IFuncProxy fp;
                    if (_actions.TryGetValue(item.ModuleType, out fp))
                    {
                        var action = item.ModuleType switch
                        {
                            ModuleType.AddDiameter => fp.GetActionWithArguments(item.Tapper),
                            ModuleType.AddZ => fp.GetActionWithArguments(item.DeltaZ),
                            ModuleType.Delay => fp.GetActionWithArguments(item.DelayTime),
                            ModuleType.Pierce => fp.GetActionWithArguments(item.MarkParams)
                        };
                        sequence.Hire(new Leaf(() => Task.Run(action)));
                    }
                    else
                    {
                        throw new KeyNotFoundException($"There is no value for {item.ModuleType} key");
                    }
                }
                else if (item.ModuleType == ModuleType.Loop)
                {
                    sequence.Hire(new Ticker(item.LoopCount).Hire(ParseModules(item.Children)));
                }
            }
            return sequence;
            //var f = new FuncProxy<Action<int>>(x => { });
        }
    }



    internal class TypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; set; }

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }

        public Type BindToType(string? assemblyName, string typeName)
        {
            return KnownTypes.SingleOrDefault(t => t.Name == typeName);
        }
    }

    public class BTBuilderX
    {
        private readonly List<IProgBlock> _progModules;
        public BTBuilderX(string jsonTree)
        {
            try
            {
                _progModules = JsonConvert.DeserializeObject<List<IProgBlock>>(jsonTree, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    SerializationBinder = new TypesBinder
                    {
                        KnownTypes = new List<Type>
                    {
                        typeof(AddZBlock),
                        typeof(DelayBlock),
                        typeof(LoopBlock),
                        typeof(PierceBlock),
                        typeof(TapperBlock),
                        typeof(MarkLaserParams),
                        typeof(PenParams),
                        typeof(HatchParams)
                    }
                    }
                }) ?? throw new ArgumentException($"Can not deserialize {nameof(jsonTree)}");
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public BTBuilderX SetModuleAction(Type type, IFuncProxy funcProxy)
        {
            _actions.TryAdd(type, funcProxy);
            return this;
        }
        private Dictionary<Type, IFuncProxy> _actions = new();


        public Sequence GetSequence()
        {
            var result = ParseModules(_progModules);
            return result;
        }
        private Sequence ParseModules(IEnumerable<IProgBlock> progModules)
        {
            var sequence = new Sequence();
            foreach (var item in progModules)
            {
                if (item.GetType() != typeof(LoopBlock))
                {
                    IFuncProxy fp;
                    if (_actions.TryGetValue(item.GetType(), out fp))
                    {
                        var action = item switch
                        {
                            TapperBlock tapperBlock => fp.GetActionWithArguments(tapperBlock.Tapper),
                            AddZBlock addZBlock => fp.GetActionWithArguments(addZBlock.DeltaZ),
                            DelayBlock delayBlock => fp.GetActionWithArguments(delayBlock.DelayTime),
                            PierceBlock pierceBlock => fp.GetActionWithArguments(pierceBlock.MarkParams),
                            _ => throw new ArgumentException($"Unknown type {nameof(item)}")
                        };
                        sequence.Hire(new Leaf(() => Task.Run(action)));
                    }
                    else
                    {
                        throw new KeyNotFoundException($"There is no value for {item.GetType()} key");
                    }
                }
                else if (item.GetType() == typeof(LoopBlock))
                {
                    sequence.Hire(new Ticker(((LoopBlock)item).LoopCount).Hire(ParseModules(((LoopBlock)item).Children)));
                }
            }
            return sequence;
            // var f = new FuncProxy<Action<int>>(x => { });
        }
    }
    public class BTBuilderY
    {
        private readonly MainLoop _progModules;
        private Dictionary<Type, IFuncProxy> _actions = new();
        public bool MainLoopShuffle { get => _progModules.Shuffle; }
        public int MainLoopCount { get => _progModules.LoopCount; }

        public BTBuilderY(string jsonTree)
        {
           
            _progModules = JsonConvert.DeserializeObject<MainLoop>(jsonTree, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = new TypesBinder
                {
                    KnownTypes = new List<Type>
                    {
                        typeof(AddZBlock),
                        typeof(DelayBlock),
                        typeof(LoopBlock),
                        typeof(PierceBlock),
                        typeof(TapperBlock),                        
                        typeof(RepairZBlock),
                        typeof(MarkLaserParams),
                        typeof(PenParams),
                        typeof(HatchParams),
                        typeof(MainLoop)
                    }
                }
            }) ?? throw new ArgumentException($"Can not deserialize {nameof(jsonTree)}");
        }

        public BTBuilderY SetModuleAction(Type type, IFuncProxy funcProxy)
        {
            _actions.TryAdd(type, funcProxy);
            return this;
        }
          
        public BTBuilderY SetModuleAction<TBlock>(IFuncProxy funcProxy) where TBlock : IProgBlock
        {
            _actions.TryAdd(typeof(TBlock), funcProxy);
            return this;
        }
        
        public ActionTree GetTree()
        {
            var result = ParseModules(_progModules.Children);
            return result;
        }

        
        private ActionTree ParseModules(IEnumerable<IProgBlock> progModules)
        {
            var mainLoop = ActionTree.StartLoop(1);
            foreach (var item in progModules)
            {
                if (item.GetType() != typeof(LoopBlock))
                {
                    IFuncProxy fp;
                    if (_actions.TryGetValue(item.GetType(), out fp))
                    {
                        var action = item switch
                        {
                            TapperBlock tapperBlock => fp.GetActionWithArguments(tapperBlock.Tapper),
                            AddZBlock addZBlock => fp.GetActionWithArguments(addZBlock.DeltaZ),
                            DelayBlock delayBlock => fp.GetActionWithArguments(delayBlock.DelayTime),
                            PierceBlock pierceBlock => fp.GetActionWithArguments(pierceBlock.MarkParams),
                            _ => throw new ArgumentException($"Unknown type {nameof(item)}")
                        };
                        mainLoop.AddChild(ActionTree.SetAction(action));
                    }
                    else
                    {
                        throw new KeyNotFoundException($"There is no value for {item.GetType()} key");
                    }
                }
                else if (item.GetType() == typeof(LoopBlock))
                {
                    var loop = (LoopBlock)item;
                    mainLoop.AddChild(ActionTree.StartLoop(loop.LoopCount).AddChild(ParseModules(loop.Children)).EndLoop);
                }
            }
            return mainLoop.EndLoop;
        }
    }

    public class BTBuilderZ
    {
        private readonly MainLoop _progModules;
        private Dictionary<Type, IFuncProxy2> _functions = new();
        public bool MainLoopShuffle { get => _progModules.Shuffle; }
        public int MainLoopCount { get => _progModules.LoopCount; }

        public BTBuilderZ(string jsonTree)
        {

            _progModules = JsonConvert.DeserializeObject<MainLoop>(jsonTree, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = new TypesBinder
                {
                    KnownTypes = new List<Type>
                    {
                        typeof(AddZBlock),
                        typeof(DelayBlock),
                        typeof(LoopBlock),
                        typeof(PierceBlock),
                        typeof(TapperBlock),
                        typeof(RepairZBlock),
                        typeof(MarkLaserParams),
                        typeof(PenParams),
                        typeof(HatchParams),
                        typeof(MainLoop)
                    }
                }
            }) ?? throw new ArgumentException($"Can not deserialize {nameof(jsonTree)}");
        }

        public BTBuilderZ SetModuleFunction<TBlock>(IFuncProxy2 funcProxy) where TBlock : IProgBlock
        {
            _functions.TryAdd(typeof(TBlock), funcProxy);
            return this;
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
                if (item.GetType() != typeof(LoopBlock))
                {
                    IFuncProxy2 fp;
                    if (_functions.TryGetValue(item.GetType(), out fp))
                    {
                        var function = item switch
                        {
                            TapperBlock tapperBlock => ((IFuncProxy2<double>)fp).GetFuncWithArguments(tapperBlock.Tapper),
                            AddZBlock addZBlock => ((IFuncProxy2<double>)fp).GetFuncWithArguments(addZBlock.DeltaZ),
                            DelayBlock delayBlock => ((IFuncProxy2<int>)fp).GetFuncWithArguments(delayBlock.DelayTime),
                            PierceBlock pierceBlock => ((IFuncProxy2<MarkLaserParams>)fp).GetFuncWithArguments(pierceBlock.MarkParams),
                            _ => throw new ArgumentException($"Unknown type {nameof(item)}")
                        };
                        mainLoop.AddChild(FuncTree.SetFunc(function));
                    }
                    else
                    {
                        throw new KeyNotFoundException($"There is no value for {item.GetType()} key");
                    }
                }
                else if (item is LoopBlock loop)
                {
                    mainLoop.AddChild(FuncTree.StartLoop(loop.LoopCount).AddChild(ParseModules(loop.Children)).EndLoop);
                }
            }
            return mainLoop.EndLoop;
        }
    }
}