using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HandyControl.Controls;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.Properties;
using PropertyChanged;

namespace NewLaserProject.ViewModels
{
    internal partial class MainViewModel
    {
        //public int FileScale { get; set; } = 1000;
        public ObservableCollection<Scale> Scales { get; set; } = new(new() { new Scale(1000, 1), new Scale(100, 1), new Scale(1, 1) });
        public Scale DefaultFileScale
        {
            get; set;
        }
        public bool IsFileLoaded { get; set; } = false;
        public bool MirrorX { get; set; } = true;
        public bool WaferTurn90 { get; set; } = true;
        public double WaferOffsetX
        {
            get; set;
        }
        public double WaferOffsetY
        {
            get; set;
        }
        [OnChangedMethod(nameof(WiferDimensionChanged))]
        public double WaferWidth { get; set; } = 48;

        [OnChangedMethod(nameof(WiferDimensionChanged))]
        public double WaferHeight { get; set; } = 60;
        public double WaferThickness { get; set; } = 0.5;

        public double XDimension
        {
            get; private set;
        }
        public double YDimension
        {
            get; private set;
        }
        public double XDimensionOffset
        {
            get; private set;
        }
        public double YDimensionOffset
        {
            get; private set;
        }
        public double CameraPosX
        {
            get; private set;
        }
        public double CameraPosY
        {
            get; private set;
        }
        [OnChangedMethod(nameof(ViewFinderChanged))]
        public double CameraViewfinderX
        {
            get; set;
        }
        [OnChangedMethod(nameof(ViewFinderChanged))]
        public double CameraViewfinderY
        {
            get; set;
        }
        public double LaserViewfinderX
        {
            get; set;
        }
        public double LaserViewfinderY
        {
            get; set;
        }
        [OnChangedMethod(nameof(CutModeSwitched))]
        public bool CutMode
        {
            get; set;
        }
        public string FileName { get; set; } = "Open the file";

        public Dictionary<string, bool> IgnoredLayers
        {
            get; set;
        }
        public LaserDbViewModel LaserDbVM
        {
            get; set;
        }
        public ObservableCollection<Material> AvailableMaterials { get; set; } = new();
        public IDictionary<string, IEnumerable<(string objType, int count)>> LayersStructure
        {
            get; private set;
        }

        private FileVM _openedFileVM;

        public int DefLayerIndex
        {
            get; set;
        }
        public int DefEntityIndex
        {
            get; set;
        }
        public int DefMaterialIndex
        {
            get; set;
        }
        public int DefTechnologyIndex
        {
            get; set;
        }

        private IDxfReader _dxfReader;


        [ICommand]
        private async Task OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "dxf files (*.dxf)|*.dxf";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = false;

            if (openFileDialog.ShowDialog() ?? false)
            {
                DefaultFileScale = Scales[0];
                FileAlignment = (FileAlignment)Alignments[0];
                //techMessager.RealeaseMessage("Загрузка...", Icon.Loading);

                //Get the path of specified file
                FileName = openFileDialog.FileName;
                if (File.Exists(FileName))
                {
                    _openedFileVM?.ResetFileView();
                    _openedFileVM.IsFileLoading = true;
                    try
                    {
                        var dxfReader = new IMDxfReader(FileName);
                        _dxfReader = new DxfEditor(dxfReader);
                        MirrorX = Settings.Default.WaferMirrorX;
                        WaferTurn90 = Settings.Default.WaferAngle90;
                        WaferOffsetX = 0;
                        WaferOffsetY = 0;
                        IgnoredLayers = new();

                        await LoadDbForFile();
                        await Task.Factory.StartNew(
                            () => _openedFileVM.SetFileView(_dxfReader, DefaultFileScale, MirrorX, WaferTurn90, WaferOffsetX, WaferOffsetY, FileName, IgnoredLayers),
                            CancellationToken.None,
                            TaskCreationOptions.None,
                            TaskScheduler.FromCurrentSynchronizationContext()
                            );
                        _openedFileVM.TransformationChanged += MainViewModel_TransformationChanged;

                        IsFileLoaded = true;
                    }
                    catch (DxfReaderException ex)
                    {
                        Growl.Error(new HandyControl.Data.GrowlInfo()
                        {
                            StaysOpen = true,
                            Message = ex.Message,
                        });
                    }                    
                }
                else
                {
                    IsFileLoaded = false;
                }
                _openedFileVM.IsFileLoading = false;
            }

        }
        private Task _loadingContextTask;
        private Task LoadContext()
        {
            return Task.WhenAll(
                    _db.Set<DefaultLayerFilter>().LoadAsync(),
                    _db.Set<Material>().LoadAsync(),
                    _db.Set<Technology>().LoadAsync(),
                    _db.Set<MaterialEntRule>().LoadAsync(),
                    _db.Set<DefaultLayerEntityTechnology>().LoadAsync()
                );
        }

        private async Task LoadDbForFile()
        {
            await Task.Run(() =>
            {
                _db.Set<DefaultLayerFilter>()
                               .AsNoTracking()
                               .ToList().ForEach(d =>
                               {
                                   IgnoredLayers[d.Filter] = d.IsVisible;
                               });
                AvailableMaterials = _db.Set<Material>()
                                        .Include(m => m.Technologies)
                                        .Include(m => m.MaterialEntRule)
                                        .AsNoTracking()
                                        .ToObservableCollection();


                LayersStructure = _dxfReader.GetLayersStructure();

                var defLayerProcDTO = ExtensionMethods.DeserilizeObject<DefaultProcessFilterDTO>(Path.Combine(ProjectPath.GetFolderPath(APP_SETTINGS_FOLDER), "DefaultProcessFilter.json"));

                if (defLayerProcDTO is not null)
                {
                    var defLayerName = _db.Set<DefaultLayerFilter>()
                        .SingleOrDefault(d => d.Id == defLayerProcDTO.LayerFilterId)?.Filter;

                    if (defLayerName is not null)
                    {
                        var layer = LayersStructure.Keys
                            .ToList()
                            .FirstOrDefault(k => k.Contains(defLayerName, StringComparison.InvariantCultureIgnoreCase));

                        if (layer is not null)
                        {
                            DefLayerIndex = LayersStructure.Keys.ToList()
                                .IndexOf(layer);

                            DefEntityIndex = LayersStructure[layer]
                                .Select(e => LaserEntDxfTypeAdapter.GetLaserEntity(e.objType))
                                .ToList()
                                .IndexOf((LaserEntity)defLayerProcDTO.EntityType);//TODO what if there is no entities on the layer
                        }
                        else
                        {
                            DefLayerIndex = 0;
                        }

                        DefMaterialIndex = AvailableMaterials
                            .ToList()
                            .FindIndex(m => m.Id == defLayerProcDTO.MaterialId);

                        var defLayerEntTechnology = _db.Set<DefaultLayerEntityTechnology>()
                            .Where(d => d.DefaultLayerFilterId == defLayerProcDTO.LayerFilterId
                            && d.EntityType == (LaserEntity)defLayerProcDTO.EntityType
                            && d.Technology.MaterialId == defLayerProcDTO.MaterialId)
                            .Select(d => d.Technology)
                            .Single();

                        DefTechnologyIndex = AvailableMaterials.Where(m => m.Id == defLayerProcDTO.MaterialId)
                            .SingleOrDefault()?
                            .Technologies?
                            .FindIndex(t => t.Id == defLayerEntTechnology.Id) ?? -1;
                    }
                }
            });
        }

        private void MainViewModel_TransformationChanged(object? sender, EventArgs e)
        {
            var fileVM = _openedFileVM as FileVM;
            if (fileVM is not null)
            {
                WaferOffsetX = fileVM.FileOffsetX;
                WaferOffsetY = fileVM.FileOffsetY;
                WaferTurn90 = fileVM.WaferTurn90;
                MirrorX = fileVM.MirrorX;
            }
        }
        private void CutModeSwitched()
        {
            if (_openedFileVM is not null)
            {
                _openedFileVM.CanCut = CutMode;
            }
        }

        [ICommand]
        private void UndoCut()
        {
            _openedFileVM?.UndoRemoveSelection();
        }
        private void WiferDimensionChanged()
        {
            var fileVM = _openedFileVM as FileVM;
            fileVM?.SetWaferDimensions(WaferWidth, WaferHeight);
        }
        private void ViewFinderChanged()
        {
            _openedFileVM?.SetViewFinders(CameraViewfinderX, CameraViewfinderY, LaserViewfinderX, LaserViewfinderY);
        }
        private void TuneMachineFileView()
        {
            XDimension = Settings.Default.XPosDimension - Settings.Default.XNegDimension;
            YDimension = Settings.Default.YPosDimension - Settings.Default.YNegDimension;
            XDimensionOffset = Settings.Default.XNegDimension;
            YDimensionOffset = Settings.Default.YNegDimension;
            CameraPosX = Settings.Default.XLeftPoint;
            CameraPosY = Settings.Default.YLeftPoint;
        }
    }
}