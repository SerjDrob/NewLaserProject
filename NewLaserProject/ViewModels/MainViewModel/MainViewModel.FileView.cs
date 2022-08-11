using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineControlsLibrary.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.Properties;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NewLaserProject.ViewModels
{
    internal partial class MainViewModel
    {
        public int FileScale { get; set; } = 1000;
        public bool MirrorX { get; set; } = true;
        public bool WaferTurn90 { get; set; } = true;
        public double WaferOffsetX { get; set; }
        public double WaferOffsetY { get; set; }
        [OnChangedMethod(nameof(WiferDimensionChanged))]
        public double WaferWidth { get; set; } = 48;
        
        [OnChangedMethod(nameof(WiferDimensionChanged))]
        public double WaferHeight { get; set; } = 60;
        public double WaferThickness { get; set; } = 0.5;
        
        public double XDimension { get; private set; }
        public double YDimension { get; private set; }
        public double XDimensionOffset { get; private set; }
        public double YDimensionOffset { get; private set; }
        public double CameraPosX { get; private set; }
        public double CameraPosY { get; private set; }
        [OnChangedMethod(nameof(ViewFinderChanged))]
        public double CameraViewfinderX { get; set; }
        [OnChangedMethod(nameof(ViewFinderChanged))]
        public double CameraViewfinderY { get; set; }
        public double LaserViewfinderX { get; set; }
        public double LaserViewfinderY { get; set; }
        
        public bool IsFileSettingsEnable { get; set; } = false;
        public string FileName { get; set; } = "Open the file";

        public Dictionary<string, bool> IgnoredLayers { get; set; }
        public LaserDbViewModel LaserDbVM { get; set; }

        public ObservableCollection<Material> AvailableMaterials { get; set; } = new();
        public IDictionary<string, IEnumerable<(string objType, int count)>> LayersStructure { get; private set; }

        private FileVM _openedFileVM;

        public int DefLayerIndex { get; set; }
        public int DefEntityIndex { get; set; }
        public int DefMaterialIndex { get; set; }
        public int DefTechnologyIndex { get; set; }

        private IDxfReader _dxfReader;


        [ICommand]
        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "dxf files (*.dxf)|*.dxf";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = false;
            
            if (openFileDialog.ShowDialog() ?? false)
            {
                //techMessager.RealeaseMessage("Загрузка...", Icon.Loading);

                //Get the path of specified file
                FileName = openFileDialog.FileName;
                if (File.Exists(FileName))
                {
                    _dxfReader = new IMDxfReader(FileName);
                    MirrorX = Settings.Default.WaferMirrorX;
                    WaferTurn90 = Settings.Default.WaferAngle90;
                    WaferOffsetX = 0;
                    WaferOffsetY = 0;
                    ((FileVM)_openedFileVM).SetFileView(_dxfReader, FileScale, MirrorX, WaferTurn90, WaferOffsetX, WaferOffsetY);
                    ((FileVM)_openedFileVM).TransformationChanged += MainViewModel_TransformationChanged;
                    
                    IgnoredLayers = new(_db.Set<DefaultLayerFilter>()
                                            .AsNoTracking()
                                            .Select(d => KeyValuePair.Create(d.Filter, d.IsVisible)));
                    
                    _db.Set<Material>()
                      .Include(m => m.Technologies)
                      .Load();
                   
                    AvailableMaterials = _db.Set<Material>()
                                            .Local
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
                                .SingleOrDefault(k => k.Contains(defLayerName, StringComparison.InvariantCultureIgnoreCase));
                            if (layer is not null)
                            {
                                DefLayerIndex = LayersStructure.Keys.ToList()
                                    .IndexOf(layer);
                                
                                DefEntityIndex = LayersStructure[layer]
                                    .Select(e => LaserEntDxfTypeAdapter.GetLaserEntity(e.objType))
                                    .ToList()
                                    .IndexOf((LaserEntity)defLayerProcDTO.EntityType);
                            }
                        }
                        else
                        {
                            DefLayerIndex = 0;
                        }

                        DefMaterialIndex = AvailableMaterials.ToList().FindIndex(m => m.Id == defLayerProcDTO.MaterialId);

                        var defLayerEntTechnology = _db.Set<DefaultLayerEntityTechnology>().ToList()
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

                    IsFileSettingsEnable = true;                   
                    
                }
                else
                {
                    IsFileSettingsEnable = false;
                }
            }

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

        private void WiferDimensionChanged()
        {
            var fileVM = _openedFileVM as FileVM;
            fileVM?.SetWaferDimensions(WaferWidth, WaferHeight);
        }
        private void ViewFinderChanged()
        {
            _openedFileVM?.SetViewFinders(CameraViewfinderX,CameraViewfinderY,LaserViewfinderX,LaserViewfinderY);
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