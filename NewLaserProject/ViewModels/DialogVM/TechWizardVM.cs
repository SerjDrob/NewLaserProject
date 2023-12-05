using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Laser.Parameters;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures;
using Newtonsoft.Json;

namespace NewLaserProject.ViewModels.DialogVM
{
    [INotifyPropertyChanged]
    public partial class TechWizardVM : DefaultDropHandler
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

        public ObservableCollection<IProgBlock> ProgBlocks
        {
            get; set;
        }
        public ObservableCollection<IProgBlock> Listing { get; set; } = new();
        public bool EditEnable { get; set; } = true;
        public IProgBlock DraggedBlock
        {
            get; set;
        }
        public IProgBlock TestBlock { get; set; } = new PierceBlock();
        public string ObjectsType
        {
            get; set;
        }
        public string ObjectsCount
        {
            get; set;
        }
        public int MainLoopCount { get; set; } = 1;
        public bool MainLoopShuffle
        {
            get; set;
        }
        private ExtendedParams _tempParams;
        private ExtendedParams _defaultParams;

        public TechWizardVM(ExtendedParams defaultParams)
        {
            ProgBlocks = new()
            {
                //new TaperBlock(),
                new PierceBlock(),
                new AddZBlock(),
                new DelayBlock(),
                new LoopBlock(),
            };
            _defaultParams = defaultParams;
        }
        public override void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is IProgBlock)
            {
                IProgBlock sourceItem = BlockFactory.GetProgBlock(dropInfo.Data);
                var sourceCollection = dropInfo.DragInfo.SourceCollection;
                if ((sourceCollection.Equals(Listing) | sourceCollection is ChildrenObservableCollection<IProgBlock>) && dropInfo.TargetCollection.TryGetList().Equals(ProgBlocks))
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

            if ((dropInfo.TargetCollection is ChildrenObservableCollection<IProgBlock> collection &&
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
        private async Task SetPiercingParams(object progModule)
        {
            if (progModule is not PierceBlock item) return;
            var @params = item?.MarkParams?.Clone() as ExtendedParams ?? _tempParams ?? _defaultParams;

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Параметры пера")
                .SetDataContext(new EditExtendedParamsVM(@params), vm => { })
                .GetCommonResultAsync<ExtendedParams>();

            if (result.Success)
            {
                _tempParams = result.CommonResult;
                item.MarkParams = (ExtendedParams)_tempParams.Clone();
            }
        }

        /// <summary>
        /// Serializes the MainLoop and saves by the path
        /// </summary>
        /// <param name="path">folder path for the saving</param>
        /// <returns>generated name of the file</returns>
        public string SaveListingToFolder(string path)
        {
            foreach (var pierceBlock in Listing.OfType<PierceBlock>())
            {
                pierceBlock.MarkParams ??= _defaultParams;
            }


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
            var listener = new TextWriterTraceListener(writer);
            listener.WriteLine(json);
            listener.Flush();
            return fileName;
        }

        public void LoadListing(string path)
        {
            var mainLoop = JsonConvert.DeserializeObject<MainLoop>(File.ReadAllText(path), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = new TypesBinder
                {
                    KnownTypes = _knownBlockTypes
                }
            });
            if (mainLoop is not null)
            {
                MainLoopCount = mainLoop.LoopCount;
                MainLoopShuffle = mainLoop.Shuffle;
                Listing = new ObservableCollection<IProgBlock>(mainLoop.Children);
            }
            else
            {
                throw new ArgumentNullException(nameof(mainLoop));
            }
        }
    }
}
