using MachineClassLibrary.BehaviourTree;
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
        private readonly List<IProgBlock> _progModules;
        private Dictionary<Type, IFuncProxy> _actions = new();

        public BTBuilderY(string jsonTree)
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
        public BTBuilderY SetModuleAction(Type type, IFuncProxy funcProxy)
        {
            _actions.TryAdd(type, funcProxy);
            return this;
        }


        public ActionTree GetTree()
        {
            var result = ParseModules(_progModules);
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
}