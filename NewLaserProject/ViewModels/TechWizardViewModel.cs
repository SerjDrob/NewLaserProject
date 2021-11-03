using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using MachineClassLibrary.BehaviourTree;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class TechWizardViewModel : DefaultDropHandler
    {
        public ObservableCollection<ProgModuleItemVM> ProgItems { get; set; } = new();
        public ObservableCollection<ProgModuleItemVM> Blocks { get; set; } = new();
        public double XItem { get; set; }
        public double YItem { get; set; }
        public ProgModuleItemVM DraggedItem { get; set; } = new() { ModuleType = ModuleType.AddDiameter };
        public bool VisibilityItem { get; set; } = true;
        public TechWizardViewModel()
        {
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.AddDiameter });
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.Pierce });
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.AddZ });
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.Loop });
        }
        public override void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is ProgModuleItemVM)
            {
                var sourceItem = dropInfo.Data as ProgModuleItemVM;
                var sourceCollection = dropInfo.DragInfo.SourceCollection;
                if ((sourceCollection.Equals(ProgItems) | sourceCollection is ChildrenObservCollection<ProgModuleItemVM>) && dropInfo.TargetCollection.TryGetList().Equals(Blocks))
                {
                    ((ObservableCollection<ProgModuleItemVM>)sourceCollection).Remove((ProgModuleItemVM)dropInfo.Data);
                }
                else if (sourceCollection.Equals(Blocks))
                {
                    var collection = dropInfo.TargetCollection.TryGetList();
                    collection.Add(new ProgModuleItemVM { CanAcceptChildren = (sourceItem.ModuleType == ModuleType.Loop), ModuleType = sourceItem.ModuleType });
                }                
                else
                {
                    base.Drop(dropInfo);
                }
            }
            VisibilityItem = false;
        }
        public override void DragOver(IDropInfo dropInfo)
        {
            if (((dropInfo.TargetCollection is ChildrenObservCollection<ProgModuleItemVM> collection && collection.MyParent.ModuleType == ModuleType.Loop)
                & (dropInfo.Data is ProgModuleItemVM itemVM1 && itemVM1.ModuleType == ModuleType.Loop)) 
                | (dropInfo.TargetCollection.TryGetList().Equals(Blocks) && dropInfo.DragInfo.SourceCollection.Equals(Blocks)))
            {
                dropInfo.Effects = DragDropEffects.None;
            }
            else
            {
               base.DragOver(dropInfo);
            }

            VisibilityItem = true;
           
        }
        [ICommand]
        private void MouseMoving(object point)
        {
            var position = (Point)point;
            XItem = position.X + 10;
            YItem = position.Y + 10;
        }
        [ICommand]
        private void CheckListing()
        {
            var json = JsonConvert.SerializeObject(ProgItems, Formatting.Indented);
            Trace.WriteLine(json);
            var btb = new BTBuilder(json);
            var rootSequence = btb.GetSequence();
        }
    }
    [INotifyPropertyChanged]
    public partial class ProgModuleItemVM
    {
        [JsonIgnore]
        public bool CanAcceptChildren { get; set; }
        public ChildrenObservCollection<ProgModuleItemVM> Children { get; private set; }
        public ProgModuleItemVM()
        {
            Children = new(this);
        }
        public void AddChild(ProgModuleItemVM child)
        {
            Children.Add(child);
        }
        public int LoopCount { get; set; }
        public double Tapper { get; set; }
        public ModuleType ModuleType { get; set; }

        [ICommand]
        private void ClickButton()
        {
            var t = LoopCount;
        }
        public bool ShouldSerializeChildren()
        {
            return ModuleType == ModuleType.Loop;
        }
        public bool ShouldSerializeLoopCount()
        {
            return ModuleType == ModuleType.Loop;
        }
        public bool ShouldSerializeTapper()
        {
            return ModuleType == ModuleType.AddDiameter;
        }
        public bool ShouldSerializeClickButtonCommand() => false;
    }
    public class ChildrenObservCollection<T> : ObservableCollection<T>
    {
        private readonly T _parent;

        [JsonIgnore]
        public T MyParent { get => _parent; }
        public ChildrenObservCollection(T parent)
        {
            _parent = parent;
        }
    }
    public enum ModuleType
    {
        Loop,
        Pierce,
        AddDiameter,
        AddZ
    }

    public class BTBuilder
    {
        private readonly List<ProgModuleItemVM> _progModules;
        public BTBuilder(string jsonTree)
        {
            _progModules = JsonConvert.DeserializeObject<List<ProgModuleItemVM>>(jsonTree);
        }
        public BTBuilder SetModuleAction(ModuleType moduleType, Action action)
        {
            _actions.TryAdd(moduleType, action);
            return this;
        }
        private Dictionary<ModuleType, Action> _actions = new Dictionary<ModuleType, Action>();


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
                    Action action;
                    if (_actions.TryGetValue(item.ModuleType, out action))
                    {
                        sequence.Hire(new Leaf(action));
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
        }
    }
}
