using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Classes;
using MachineControlsLibrary.CommonDialog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Get;
using NewLaserProject.Data.Models.MaterialFeatures.Get;
using NewLaserProject.Properties;
using NewLaserProject.ViewModels.DialogVM;
using PropertyChanged;
using CommonDialog = MachineControlsLibrary.CommonDialog.CommonDialog;

namespace NewLaserProject.ViewModels
{
    internal partial class MainViewModel
    {
        public Scale DefaultFileScale { get; set; } = Scale.ThousandToOne;
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
        [OnChangedMethod(nameof(WaferDimensionChanged))]
        public double WaferWidth { get; set; } = 48;

        [OnChangedMethod(nameof(WaferDimensionChanged))]
        public double WaferHeight { get; set; } = 60;
        public double WaferThickness { get; set; } = 0.5;

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
        public string FileName
        {
            get; set;
        }

        public Dictionary<string, bool> IgnoredLayers
        {
            get; set;
        }
        public LaserDbViewModel LaserDbVM
        {
            get; set;
        }

        private FileVM _openedFileVM;
        public bool CanUndoCut
        {
            get; private set;
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
                        _logger.LogInformation(new EventId(1, "Dxf file broken"), ex, $"Swallowed the exception in the {nameof(MainViewModel.OpenFile)} method.");

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
        //private Task LoadContext()
        //{
        //    return Task.WhenAll(
        //            _db.Set<DefaultLayerFilter>().LoadAsync(),
        //            _db.Set<Material>().LoadAsync(),
        //            _db.Set<Technology>().LoadAsync(),
        //            _db.Set<MaterialEntRule>().LoadAsync(),
        //            _db.Set<DefaultLayerEntityTechnology>().LoadAsync()
        //        );
        //}

        public LayersProcessingModel LayersProcessingModel
        {
            get;
            set;
        }

        private async Task LoadDbForFile()
        {
            var response = await _mediator.Send(new GetFullMaterialHasTechnologyRequest());
            var availableMaterials = response.Materials;
            var defLayerResponse = await _mediator.Send(new GetDefaultLayerFiltersFullRequest());
            var defLayerFilters = defLayerResponse.DefaultLayerFilters;
            await Task.Run(() =>
            {
                defLayerFilters.ToList().ForEach(d =>
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