using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.Diagnostics;
using MachineClassLibrary.BehaviourTree;
using Newtonsoft.Json.Linq;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class TechWizardViewModel : DefaultDropHandler
    {
        public ObservableCollection<ProgModuleItemVM> ProgItems { get; set; } = new();
        public ObservableCollection<ProgModuleItemVM> Blocks { get; set; } = new();        
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
                if (sourceCollection.Equals(Blocks))
                {
                    var collection = dropInfo.TargetCollection.TryGetList();
                    collection.Add(new ProgModuleItemVM { CanAcceptChildren = (sourceItem.ModuleType == ModuleType.Loop), ModuleType = sourceItem.ModuleType });
                }
                else
                {
                    base.Drop(dropInfo);
                }
            }
        }
        public override void DragOver(IDropInfo dropInfo)
        {
            if ((dropInfo.TargetCollection is ChildrenObservCollection<ProgModuleItemVM> collection && collection.MyParent.ModuleType == ModuleType.Loop)
                & (dropInfo.Data is ProgModuleItemVM itemVM1 && itemVM1.ModuleType == ModuleType.Loop))
            {
                dropInfo.Effects = DragDropEffects.None;
            }
            else
            {
                base.DragOver(dropInfo);
            }
        }

        [ICommand]
        private void CheckListing()
        {
            var json = JsonConvert.SerializeObject(ProgItems,Formatting.Indented);
            Trace.WriteLine(json);
            var btb = new BTBuilder(json);
            btb.GetSequence();
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
        public BTBuilder SetModuleAction(ModuleType moduleType, Action<NumPar> action) { return this; }
        public class NumPar
        {

        }
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
                    sequence.Hire(new Leaf(() => { }));
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
