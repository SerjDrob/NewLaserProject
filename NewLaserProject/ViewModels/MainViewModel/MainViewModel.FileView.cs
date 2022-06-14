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
        public double WaferWidth { get; set; } = 48;
        public double WaferHeight { get; set; } = 60;
        public double WaferThickness { get; set; } = 0.5;
        public double WaferMargin { get; set; } = 0.2;
        public double FileSizeX { get; set; }
        public double FileSizeY { get; set; }
        public double FieldSizeX { get => FileScale * WaferWidth; }
        public double FieldSizeY { get => FileScale * WaferHeight; }
        public double XDimension { get; private set; }
        public double YDimension { get; private set; }
        public double XDimensionOffset { get; private set; }
        public double YDimensionOffset { get; private set; }
        public double CameraPosX { get; private set; }
        public double CameraPosY { get; private set; }
        public double CameraViewfinderX { get; set; }
        public double CameraViewfinderY { get; set; }
        public double LaserViewfinderX { get; set; }
        public double LaserViewfinderY { get; set; }
        public double LaserCameraOffsetX { get; set; } = -5;
        public double LaserCameraOffsetY { get; set; } = 1;
        public bool WaferContourVisibility { get; set; } = true;
        public bool IsFileSettingsEnable { get; set; } = false;
        public string FileName { get; set; } = "Open the file";

        public Dictionary<string, bool> IgnoredLayers { get; set; }
        public LaserDbViewModel LaserDbVM { get; set; }

        public ObservableCollection<LayerGeometryCollection> LayGeoms { get; set; } = new();
        public ObservableCollection<Material> AvailableMaterials { get; set; } = new();
        public IDictionary<string, IEnumerable<(string objType, int count)>> LayersStructure { get; private set; }

        public int DefLayerIndex { get; set; }
        public int DefEntityIndex { get; set; }
        public int DefMaterialIndex { get; set; }
        public int DefTechnologyIndex { get; set; }

        private IDxfReader _dxfReader;


        [ICommand]
        private void AlignWafer(object obj)
        {
            var param = (ValueTuple<object, object, object>)obj;
            var scaleX = (double)param.Item1;
            var scaleY = (double)param.Item2;
            var aligning = (Aligning)param.Item3;
            var size = _dxfReader.GetSize();
            var dx = WaferWidth * FileScale;
            var dy = WaferHeight * FileScale;

            WaferOffsetX = aligning switch
            {
                Aligning.Right or Aligning.RTCorner or Aligning.RBCorner => -(WaferTurn90 ? (size.height - dx) : (size.width - dx)) * scaleX / 2,
                Aligning.Left or Aligning.LTCorner or Aligning.LBCorner => (WaferTurn90 ? (size.height - dx) : (size.width - dx)) * scaleX / 2,
                Aligning.Top or Aligning.Bottom or Aligning.Center => 0,
            };

            WaferOffsetY = aligning switch
            {
                Aligning.Top or Aligning.RTCorner or Aligning.LTCorner => (WaferTurn90 ? (size.width - dy) : (size.height - dy)) * scaleY / 2,
                Aligning.Bottom or Aligning.RBCorner or Aligning.LBCorner => -(WaferTurn90 ? (size.width - dy) : (size.height - dy)) * scaleY / 2,
                Aligning.Right or Aligning.Left or Aligning.Center => 0
            };

        }

        [ICommand]
        private void ChangeMirrorX() => MirrorX ^= true;

        [ICommand]
        private void ChangeTurn90() => WaferTurn90 ^= true;

        [ICommand]
        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "d:\\";
            openFileDialog.Filter = "dxf files (*.dxf)|*.dxf";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() ?? false)
            {
                //techMessager.RealeaseMessage("Загрузка...", Icon.Loading);

                //Get the path of specified file
                FileName = openFileDialog.FileName;
                if (File.Exists(FileName))
                {
                    _dxfReader = new IMDxfReader(FileName);

                    //var curveEnumerator = _dxfReader.GetAllDxfCurves(Path.Combine(_projectDirectory, "TempFiles")).GetEnumerator();
                    //var c = curveEnumerator.MoveNext();
                    //var curve = curveEnumerator.Current;


                    //var segments = _dxfReader.GetAllSegments();
                    var fileSize = _dxfReader.GetSize();
                    FileSizeX = Math.Round(fileSize.width);
                    FileSizeY = Math.Round(fileSize.height);
                    IgnoredLayers = new(_db.Set<DefaultLayerFilter>()
                                            .AsNoTracking()
                                            .Select(d => KeyValuePair.Create(d.Filter, d.IsVisible)));
                    LayGeoms = new LayGeomAdapter(_dxfReader).LayerGeometryCollections;
                    
                    _db.Set<Material>()
                      .Include(m => m.Technologies)
                      .Load();
                    AvailableMaterials = _db.Set<Material>().Local.ToObservableCollection();

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
                    
                    //LPModel = new(_dxfReader);
                    //TWModel = new();                  



                    //LPModel.ObjectChosenEvent += TWModel.SetObjectsTC;

                    MirrorX = Settings.Default.WaferMirrorX;
                    WaferTurn90 = Settings.Default.WaferAngle90;
                    WaferOffsetX = 0;
                    WaferOffsetY = 0;
                }
                else
                {
                    IsFileSettingsEnable = false;
                }
                // techMessager.EraseMessage();
            }

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