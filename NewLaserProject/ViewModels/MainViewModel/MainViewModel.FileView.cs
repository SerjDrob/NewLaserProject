﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Classes;
using MachineControlsLibrary.Controls.GraphWin;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Get;
using NewLaserProject.Data.Models.MaterialFeatures.Get;
using NewLaserProject.ViewModels.DialogVM;
using PropertyChanged;
using CommonDialog = MachineControlsLibrary.CommonDialog.CommonDialog;

namespace NewLaserProject.ViewModels
{
    public partial class MainViewModel
    {
        private FileVM _openedFileVM;
        private IDxfReader _dxfReader;
        private (PointF minPoint, PointF maxPoint) _fileActualSize;

        [OnChangedMethod(nameof(FileScaleChanged))]
        public Scale DefaultFileScale { get; set; } = Scale.ThousandToOne;
        public bool IsFileLoaded { get; set; } = false;
        public bool MirrorX { get; set; } = true;
        public bool WaferTurn90 { get; set; } = true;
        public double WaferOffsetX { get; set; }
        public double WaferOffsetY { get; set; }

        [OnChangedMethod(nameof(WaferDimensionChanged))]
        public double WaferWidth { get; set; }

        [OnChangedMethod(nameof(WaferDimensionChanged))]
        public double WaferHeight { get; set; }
        public double WaferThickness { get; set; }

        [OnChangedMethod(nameof(ViewFinderChanged))]
        public double CameraViewfinderX { get; set; }
        [OnChangedMethod(nameof(ViewFinderChanged))]
        public double CameraViewfinderY { get; set; }
        public double LaserViewfinderX { get; set; }
        public double LaserViewfinderY { get; set; }
        [OnChangedMethod(nameof(CutModeSwitched))]
        public bool CutMode { get; set; }
        public string FileName { get; set; }

        public Dictionary<string, bool> IgnoredLayers { get; set; }
        public LaserDbViewModel LaserDbVM { get; set; }

        
        public bool CanUndoCut { get; private set; }

        
        private void FileScaleChanged()
        {
            if (_openedFileVM is not null) _openedFileVM.FileScale = DefaultFileScale;
        }

        [ICommand]
        private async Task OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "dxf files (*.dxf)|*.dxf";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = false;

            if (openFileDialog.ShowDialog() ?? false)
            {
                FileName = openFileDialog.FileName;
                await OpenChosenFile();
            }

        }

        private async Task OpenChosenFile(bool byWPU = false, params double[] offsets)
        {

            if (File.Exists(FileName))
            {
                _openedFileVM?.ResetFileView();
                _openedFileVM.IsFileLoading = true;
                try
                {
                    var dxfReader = new IMDxfReader(FileName);
                    _dxfReader = new DxfEditor(dxfReader);
                    _fileActualSize = _dxfReader.GetSize2();
                    if (!byWPU)
                    {
                        MirrorX = _settingsManager.Settings.WaferMirrorX ?? throw new ArgumentNullException("WaferMirrorX is null");
                        WaferTurn90 = _settingsManager.Settings.WaferAngle90 ?? throw new ArgumentNullException("WaferAngle90 is null");
                    }
                    var (wox, woy, fox, foy) = (0d,0d,0d,0d);
                    if (byWPU)
                    {
                        _openedFileVM.TextPosition = (TextPosition)MarkPosition;
                        wox = offsets.ElementAtOrDefault(0);
                        woy = offsets.ElementAtOrDefault(1);
                        fox = offsets.ElementAtOrDefault(2);
                        foy = offsets.ElementAtOrDefault(3);
                    }
                    if(!byWPU) IgnoredLayers = new();
                    await LoadDbForFile(byWPU);
                    await Task.Factory.StartNew(
                        () => _openedFileVM.SetFileView(_dxfReader, DefaultFileScale, MirrorX, WaferTurn90, 
                        wox, woy, 
                        fox, foy,
                        FileName, IgnoredLayers),
                        CancellationToken.None,
                        TaskCreationOptions.None,
                        TaskScheduler.FromCurrentSynchronizationContext()
                        );
                    _openedFileVM.TransformationChanged += MainViewModel_TransformationChanged;

                    IsFileLoaded = true;
                    _markTextVM = new() 
                    {
                        MarkedText = Path.GetFileNameWithoutExtension(FileName),
                        FileName = Path.GetFileNameWithoutExtension(FileName)
                    };
                }
                catch (DxfReaderException ex)
                {
                    //_logger.LogInformation(new EventId(1, "Dxf file broken"), ex, $"Swallowed the exception in the {nameof(MainViewModel.OpenFile)} method.");
                    _logger.ForContext<MainViewModel>().Warning(ex, $"Swallowed the exception in the {nameof(MainViewModel.OpenFile)} method.");

                    Growl.Error(new HandyControl.Data.GrowlInfo()
                    {
                        StaysOpen = true,
                        Message = ex.Message,
                    });
                }
                catch(Exception ex)
                {

                }
                finally
                {
                    ExistingAlignment = null;
                    IsPrevAlignmentEnable = false;
                }
            }
            else
            {
                IsFileLoaded = false;
            }
            _openedFileVM.IsFileLoading = false;
        }

        public LayersProcessingModel LayersProcessingModel { get; set; }


        [ICommand]
        private void DeleteProcObj(ObjectForProcessing obj)
        {
            ChosenProcessingObjects?.Remove(obj);
            LayersProcessingModel?.UnCheckItem((obj.Layer ?? "", obj.LaserEntity));
        }

        private async Task LoadDbForFile(bool byWPU)
        {
            var response = await _mediator.Send(new GetFullMaterialHasTechnologyRequest());
            var availableMaterials = response.Materials;
            var defLayerResponse = await _mediator.Send(new GetDefaultLayerFiltersFullRequest());
            var defLayerFilters = defLayerResponse.DefaultLayerFilters;
            await Task.Run(() =>
            {
                if(!byWPU) defLayerFilters.ToList().ForEach(d =>
                               {
                                   IgnoredLayers[d.Filter] = d.IsVisible;
                               });

                LayersProcessingModel?.UnSubscribe();
                LayersProcessingModel = new(_dxfReader);
                ChosenProcessingObjects = new();
                LayersProcessingModel.Select(args => Observable.FromAsync(async () =>
                {
                    if (!args.isCheck)
                    {
                        var item = ChosenProcessingObjects?.SingleOrDefault(o => o.Layer == args.layerName && o.LaserEntity == args.entType);
                        ChosenProcessingObjects?.Remove(item);
                    }
                    else
                    {
                        var tempTechnology = ChosenProcessingObjects.LastOrDefault()?.Technology ?? availableMaterials?.First()?.Technologies?.First();
                        var result = await Dialog.Show<CommonDialog>()
                            .SetDialogTitle("Выбор технологии обработки")
                            .SetDataContext<AddProcObjectsVM>(vm =>
                            {
                                vm.ObjectForProcessing = new()
                                {
                                    Layer = args.layerName,
                                    LaserEntity = args.entType,
                                    Technology = tempTechnology
                                };
                                vm.Material = tempTechnology?.Material;
                                vm.Materials = new(availableMaterials);
                            })
                            .GetCommonResultAsync<ObjectForProcessing>();
                        if (result.Success)
                        {
                            ChosenProcessingObjects.Add(result.CommonResult);
                        }
                        else
                        {
                            var (x, y, _) = args;
                            LayersProcessingModel.UnCheckItem((x, y));
                        }
                    }

                }))
                .Concat()
                .Subscribe();
            });
        }
        private void MainViewModel_TransformationChanged(object? sender, EventArgs e)
        {
            var fileVM = _openedFileVM;
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
        private void WaferDimensionChanged()
        {
            var fileVM = _openedFileVM as FileVM;
            fileVM?.SetWaferDimensions(WaferWidth, WaferHeight);
        }
        private void ViewFinderChanged()
        {
            _openedFileVM?.SetViewFinders(CameraViewfinderX, CameraViewfinderY, LaserViewfinderX, LaserViewfinderY);
        }
    }
}
