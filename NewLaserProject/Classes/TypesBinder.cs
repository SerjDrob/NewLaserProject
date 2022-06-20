using MachineClassLibrary.Laser;
using NewLaserProject.Classes.ProgBlocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLaserProject.Classes
{
    //public class BTBuilder
    //{
    //    private readonly List<ProgModuleItemVM> _progModules;
    //    public BTBuilder(string jsonTree)
    //    {
    //        _progModules = JsonConvert.DeserializeObject<List<ProgModuleItemVM>>(jsonTree);
    //    }
    //    public BTBuilder SetModuleAction(ModuleType moduleType, IFuncProxy funcProxy)
    //    {
    //        _actions.TryAdd(moduleType, funcProxy);
    //        return this;
    //    }
    //    private Dictionary<ModuleType, IFuncProxy> _actions = new();


    //    public Sequence GetSequence()
    //    {
    //        var result = ParseModules(_progModules);
    //        return result;
    //    }
    //    private Sequence ParseModules(IEnumerable<ProgModuleItemVM> progModules)
    //    {
    //        var sequence = new Sequence();
    //        foreach (var item in progModules)
    //        {
    //            if (item.ModuleType != ModuleType.Loop)
    //            {
    //                IFuncProxy fp;
    //                if (_actions.TryGetValue(item.ModuleType, out fp))
    //                {
    //                    var action = item.ModuleType switch
    //                    {
    //                        ModuleType.AddDiameter => fp.GetActionWithArguments(item.Tapper),
    //                        ModuleType.AddZ => fp.GetActionWithArguments(item.DeltaZ),
    //                        ModuleType.Delay => fp.GetActionWithArguments(item.DelayTime),
    //                        ModuleType.Pierce => fp.GetActionWithArguments(item.MarkParams)
    //                    };
    //                    sequence.Hire(new Leaf(() => Task.Run(action)));
    //                }
    //                else
    //                {
    //                    throw new KeyNotFoundException($"There is no value for {item.ModuleType} key");
    //                }
    //            }
    //            else if (item.ModuleType == ModuleType.Loop)
    //            {
    //                sequence.Hire(new Ticker(item.LoopCount).Hire(ParseModules(item.Children)));
    //            }
    //        }
    //        return sequence;
    //        //var f = new FuncProxy<Action<int>>(x => { });
    //    }
    //}



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


    internal class JsonDeserializer<TObject>
    {
        private List<Type> _knownTypes;
        public JsonDeserializer()
        {
            _knownTypes = new();
            _knownTypes.Add(typeof(TObject));
        }
        public JsonDeserializer<TObject> SetKnownType<TKnown>()
        {
            _knownTypes.Add(typeof(TKnown));
            return this;
        }
        public TObject Deserialize(string jsonTree)
        {
            TObject result;
            try
            {
                result = JsonConvert.DeserializeObject<TObject>(jsonTree, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    SerializationBinder = new TypesBinder
                    {
                        KnownTypes = _knownTypes
                    }
                }) ?? throw new ArgumentException($"Can not deserialize {nameof(jsonTree)}");
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;
        }
    }
        
}