using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using MachineClassLibrary.BehaviourTree;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MachineClassLibrary.Laser;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class TechWizardViewModel : DefaultDropHandler
    {
        public ObservableCollection<ProgModuleItemVM> ProgItems { get; set; } = new();
        public ObservableCollection<ProgModuleItemVM> Blocks { get; set; } = new();

        public ObservableCollection<IProgBlock> ProgBlocks { get; set; }
        public ObservableCollection<IProgBlock> Listing { get; set; } = new();
        public IProgBlock DraggedBlock { get; set; }
        public IProgBlock TestBlock { get; set; } = new PierceBlock();
        public string ObjectsType { get; set; }
        public string ObjectsCount { get; set; }
        public TechWizardViewModel()
        {
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.AddDiameter });
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.Pierce });
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.AddZ });
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.Loop });
            Blocks.Add(new ProgModuleItemVM { ModuleType = ModuleType.Delay });

            ProgBlocks = new()
            {
                new TapperBlock(),
                new PierceBlock(),
                new AddZBlock(),
                new LoopBlock(),
                new DelayBlock()
            };
        }
        public override void Drop(IDropInfo dropInfo)
        {
            //if (dropInfo.Data is ProgModuleItemVM)
            //{
            //    var sourceItem = dropInfo.Data as ProgModuleItemVM;
            //    var sourceCollection = dropInfo.DragInfo.SourceCollection;
            //    if ((sourceCollection.Equals(ProgItems) | sourceCollection is ChildrenObservCollection<ProgModuleItemVM>) && dropInfo.TargetCollection.TryGetList().Equals(Blocks))
            //    {
            //        ((ObservableCollection<ProgModuleItemVM>)sourceCollection).Remove((ProgModuleItemVM)dropInfo.Data);
            //    }
            //    else if (sourceCollection.Equals(Blocks))
            //    {
            //        var collection = dropInfo.TargetCollection.TryGetList();
            //        collection.Add(new ProgModuleItemVM { CanAcceptChildren = (sourceItem.ModuleType == ModuleType.Loop), ModuleType = sourceItem.ModuleType });
            //    }
            //    else
            //    {
            //        base.Drop(dropInfo);
            //    }
            //}



            if (dropInfo.Data is IProgBlock)
            {
                IProgBlock sourceItem = BlockFactory.GetProgBlock(dropInfo.Data);
                var sourceCollection = dropInfo.DragInfo.SourceCollection;
                if ((sourceCollection.Equals(Listing) | sourceCollection is ChildrenObservCollection<IProgBlock>) && dropInfo.TargetCollection.TryGetList().Equals(ProgBlocks))
                {
                    ((ObservableCollection<IProgBlock>)sourceCollection).Remove(sourceItem);
                }
                else if (sourceCollection.Equals(ProgBlocks))
                {
                    var collection = dropInfo.TargetCollection.TryGetList();
                    collection.Add(BlockFactory.BlockTypeSelector(sourceItem));
                }
                else
                {
                    base.Drop(dropInfo);
                }
                DraggedBlock = null;
            }
        }
        public override void DragOver(IDropInfo dropInfo)
        {
            //    if (((dropInfo.TargetCollection is ChildrenObservCollection<ProgModuleItemVM> collection && collection.MyParent.ModuleType == ModuleType.Loop)
            //        & (dropInfo.Data is ProgModuleItemVM itemVM1 && itemVM1.ModuleType == ModuleType.Loop))
            //        | (dropInfo.TargetCollection.TryGetList().Equals(Blocks) && dropInfo.DragInfo.SourceCollection.Equals(Blocks)))
            //    {
            //        dropInfo.Effects = DragDropEffects.None;
            //    }
            //    else
            //    {
            //        base.DragOver(dropInfo);
            //    }

            //if (DraggedBlock is null)
            //{
            DraggedBlock = BlockFactory.BlockTypeSelector(dropInfo.DragInfo.SourceItem);
            // }

            if ((dropInfo.TargetCollection is ChildrenObservCollection<IProgBlock> collection &&
                BlockFactory.GetProgBlock(collection.MyParent).Equals(BlockFactory.GetProgBlock(dropInfo.Data)))
                | (dropInfo.TargetCollection.TryGetList().Equals(ProgBlocks) && dropInfo.DragInfo.SourceCollection.Equals(ProgBlocks)))
            {
                dropInfo.Effects = DragDropEffects.None;
            }
            else
            {
                base.DragOver(dropInfo);
            }
        }
        public void SetObjectsTC(object obj, string[] tc)
        {
            if (tc.Length > 1)
            {
                ObjectsType = tc[0];
                ObjectsCount = tc[1];
            }
            var path = ObjectsType switch
            {
                "Circle" => "D:/CircleListing.json",
                "LightWeightPolyline" => "D:/PolylineListing.json",
                "Line" => "D:/LineListing.json"
            };
            if (File.Exists(path))
            {
                var listing = JsonConvert.DeserializeObject(File.ReadAllText(path), typeof(ObservableCollection<ProgModuleItemVM>));
                ProgItems = (ObservableCollection<ProgModuleItemVM>)listing;
            }
            else
            {
                ProgItems = new();
            }
        }
        [ICommand]
        private void SetPiercingParams(object progModule)
        {
            var item = (ProgModuleItemVM)progModule;
            var markSettings = new MarkSettingsViewModel();
            new MarkSettingsView { DataContext = markSettings }.ShowDialog();
            item.MarkParams = markSettings.GetLaserParams();
        }

        [ICommand]
        private void SaveListing()
        {
            var json = JsonConvert.SerializeObject(ProgItems, Formatting.Indented);
            //Trace.WriteLine(json);
            using var writer = new StreamWriter("D:/CircleListing.json", false);
            var l = new TextWriterTraceListener(writer);
            l.WriteLine(json);
            l.Flush();
        }
        [ICommand]
        private void LoadListing()
        {
            var listing = JsonConvert.DeserializeObject(File.ReadAllText("D:/CircleListing.json"), typeof(ObservableCollection<ProgModuleItemVM>));
            ProgItems = (ObservableCollection<ProgModuleItemVM>)listing;
        }
        [ICommand]
        private void CheckListing()
        {
            var json = JsonConvert.SerializeObject(Listing,Formatting.Indented,new JsonSerializerSettings 
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
                        typeof(TapperBlock)
                    }
                }
            });
            Trace.WriteLine(json);
            var btb = new BTBuilderX(json);
            var rootSequence = btb.SetModuleAction(typeof(TapperBlock), new FuncProxy<Action<double>>(x => Console.WriteLine(x)))
                                  .SetModuleAction(typeof(AddZBlock), new FuncProxy<Action<double>>(z => Console.WriteLine(z)))
                                  .SetModuleAction(typeof(DelayBlock), new FuncProxy<Action<int>>(z => Console.WriteLine(z)))
                                  .SetModuleAction(typeof(LoopBlock), new FuncProxy<Action<int>>(z => Console.WriteLine(z)))
                                  .SetModuleAction(typeof(PierceBlock), new FuncProxy<Action<MarkLaserParams>>(z => Console.WriteLine(z)))
                                  .GetSequence();
        }
    }
}
