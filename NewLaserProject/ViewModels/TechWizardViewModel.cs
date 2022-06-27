using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using MachineClassLibrary.Laser.Parameters;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class TechWizardViewModel : DefaultDropHandler
    {
        private readonly List<Type> _knownBlockTypes = new()
        {
            typeof(AddZBlock),
            typeof(DelayBlock),
            typeof(LoopBlock),
            typeof(PierceBlock),
            typeof(TaperBlock),
            typeof(MarkLaserParams),
            typeof(PenParams),
            typeof(HatchParams),
            typeof(MainLoop),
            typeof(ExtendedParams)
        };

        public ObservableCollection<IProgBlock> ProgBlocks { get; set; }
        public ObservableCollection<IProgBlock> Listing { get; set; } = new();
        public bool EditEnable { get; set; } = true;
        public IProgBlock DraggedBlock { get; set; }
        public IProgBlock TestBlock { get; set; } = new PierceBlock();
        public string ObjectsType { get; set; }
        public string ObjectsCount { get; set; }
        public int MainLoopCount { get; set; } = 1;
        public bool MainLoopShuffle { get; set; }
        private IMapper _markParamsToMSVMMapper;
        public TechWizardViewModel()
        {
            ProgBlocks = new()
            {
                new TaperBlock(),
                new PierceBlock(),
                new AddZBlock(),
                new LoopBlock(),
                new DelayBlock(),
            };
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MarkLaserParams, MarkSettingsViewModel>()
                .IncludeMembers(s => s.PenParams, s => s.HatchParams);
                cfg.CreateMap<PenParams, MarkSettingsViewModel>(MemberList.None);
                cfg.CreateMap<HatchParams, MarkSettingsViewModel>(MemberList.None);

            });
            _markParamsToMSVMMapper = config.CreateMapper();
        }
        public override void Drop(IDropInfo dropInfo)
        {
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

            DraggedBlock = BlockFactory.BlockTypeSelector(dropInfo.DragInfo.SourceItem);

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
                var listing = JsonConvert.DeserializeObject<List<IProgBlock>>(File.ReadAllText(path), new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    SerializationBinder = new TypesBinder
                    {
                        KnownTypes = _knownBlockTypes
                    }
                });

                Listing = new ObservableCollection<IProgBlock>(listing);
            }
            else
            {
                Listing = new();
            }
        }
        [ICommand]
        private void SetPiercingParams(object progModule)
        {
            //var item = (PierceBlock)progModule;
            //if (item.MarkParams is null)
            //{
            //   var markSettings = new MarkSettingsViewModel();
            //    new MarkSettingsView { DataContext = markSettings }.ShowDialog();
            //    item.MarkParams = markSettings.GetLaserParams();
            //}
            //else
            //{                
            //    var markSettingsVM = _markParamsToMSVMMapper.Map<MarkSettingsViewModel>(item.MarkParams);
            //    new MarkSettingsView { DataContext = markSettingsVM }.ShowDialog();
            //    item.MarkParams = markSettingsVM.GetLaserParams();
            //}



            var item = (PierceBlock)progModule;
            if (item.MarkParams is null)
            {
                var markSettings = new ExtendedParams();
                new ExtMarkParamsView { DataContext = markSettings }.ShowDialog();
                item.MarkParams = markSettings;
            }
            else
            {
                var markSettings = item.MarkParams;
                new ExtMarkParamsView { DataContext = markSettings }.ShowDialog();
                item.MarkParams = markSettings;
            }

            //var extParamsView = new ExtMarkParamsView
            //{
            //    DataContext = new ExtendedParams()
            //}.ShowDialog();
        }

        //[ICommand]
        //private void SaveListing()
        //{
        //    var mainLoop = new MainLoop(MainLoopCount,MainLoopShuffle,Listing);
            
        //    var json = JsonConvert.SerializeObject(mainLoop, Formatting.Indented, new JsonSerializerSettings
        //    {
        //        TypeNameHandling = TypeNameHandling.Objects,
        //        SerializationBinder = new TypesBinder
        //        {
        //            KnownTypes = _knownBlockTypes
        //        }
        //    });
        //    using var writer = new StreamWriter($"{_projectDirectory}/TechnologyFiles/CircleListing.json", false);

        //    var l = new TextWriterTraceListener(writer);
        //    l.WriteLine(json);
        //    l.Flush();
        //}

        /// <summary>
        /// Serializes the MainLoop and saves by the path
        /// </summary>
        /// <param name="path">folder path for the saving</param>
        /// <returns>generated name of the file</returns>
        public string SaveListingToFolder(string path)
        {
            var mainLoop = new MainLoop(MainLoopCount, MainLoopShuffle, Listing);

            var json = JsonConvert.SerializeObject(mainLoop, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = new TypesBinder
                {
                    KnownTypes = _knownBlockTypes
                }
            });
            var fileName = Guid.NewGuid().ToString();
            using var writer = new StreamWriter(Path.Combine(path, $"{fileName}.json"), false);
            var l = new TextWriterTraceListener(writer);
            l.WriteLine(json);
            l.Flush();
            return fileName;
        }

        //[ICommand]
        //private void LoadListing()
        //{
        //    //var listing = JsonConvert.DeserializeObject<ObservableCollection<ProgModuleItemVM>>(File.ReadAllText($"{_projectDirectory}/TechnologyFiles/CircleListing.json"));
        //    //Listing = (ObservableCollection<IProgBlock>)listing;
        //    //Listing = new(listing);

        //    var listing = JsonConvert.DeserializeObject<IProgBlock>(File.ReadAllText($"{_projectDirectory}/TechnologyFiles/CircleListing.json"), new JsonSerializerSettings
        //    {
        //        TypeNameHandling = TypeNameHandling.Objects,
        //        SerializationBinder = new TypesBinder
        //        {
        //            KnownTypes = _knownBlockTypes
        //        }
        //    });

        //    //Listing = new ObservableCollection<IProgBlock>(listing);
        //}

        public void LoadListing(string path)
        {
            var mainloop = JsonConvert.DeserializeObject<MainLoop>(File.ReadAllText(path), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = new TypesBinder
                {
                    KnownTypes = _knownBlockTypes
                }
            });
            if (mainloop is not null)
            {
                MainLoopCount = mainloop.LoopCount;
                MainLoopShuffle = mainloop.Shuffle;
                Listing = new ObservableCollection<IProgBlock>(mainloop.Children); 
            }
            else
            {
                throw new ArgumentNullException(nameof(mainloop));
            }
        }

        //[ICommand]
        //private void CheckListing()
        //{
        //    var json = JsonConvert.SerializeObject(Listing, Formatting.Indented, new JsonSerializerSettings
        //    {
        //        TypeNameHandling = TypeNameHandling.Objects,
        //        SerializationBinder = new TypesBinder
        //        {
        //            KnownTypes = _knownBlockTypes
        //        }
        //    });
        //    Trace.WriteLine(json);
        //    var btb = new BTBuilderX(json);
        //    var rootSequence = btb.SetModuleAction(typeof(TapperBlock), new FuncProxy<Action<double>>(x => Console.WriteLine(x)))
        //                          .SetModuleAction(typeof(AddZBlock), new FuncProxy<Action<double>>(z => Console.WriteLine(z)))
        //                          .SetModuleAction(typeof(DelayBlock), new FuncProxy<Action<int>>(z => Console.WriteLine(z)))
        //                          .SetModuleAction(typeof(LoopBlock), new FuncProxy<Action<int>>(z => Console.WriteLine(z)))
        //                          .SetModuleAction(typeof(PierceBlock), new FuncProxy<Action<MarkLaserParams>>(z => Console.WriteLine(z)))
        //                          .GetSequence();
        //}
    }
}
