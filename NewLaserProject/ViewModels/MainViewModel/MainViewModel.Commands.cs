using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models.DefaultLayerEntityTechnologyFeatures.Get;
using NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Create;
using NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Delete;
using NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Get;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.ViewModels.DialogVM;

namespace NewLaserProject.ViewModels
{
    public partial class MainViewModel
    {
        public ICommand? TestKeyCommand
        {
            get; protected set;
        }
        public double TestX
        {
            get; set;
        }
        public double TestY
        {
            get; set;
        }
        private void InitCommands()
        {

            TestKeyCommand = new KeyProcessorCommands(parameter => true, typeof(TextBox))
                //.CreateAnyKeyDownCommand(moveAsync, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.A, () => moveAxDirAsync((Ax.Y, AxDir.Pos)), () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Z, () => moveAxDirAsync((Ax.Y, AxDir.Neg)), () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.X, () => moveAxDirAsync((Ax.X, AxDir.Neg)), () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.C, () => moveAxDirAsync((Ax.X, AxDir.Pos)), () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.V, () => moveAxDirAsync((Ax.Z, AxDir.Pos)), () => IsMainTabOpen)
                .CreateKeyDownCommand(Key.B, () => moveAxDirAsync((Ax.Z, AxDir.Neg)), () => IsMainTabOpen)
                .CreateKeyUpCommand(Key.V, () => Task.Run(() => _laserMachine.Stop(Ax.Z)), () => IsMainTabOpen)
                .CreateKeyUpCommand(Key.B, () => Task.Run(() => _laserMachine.Stop(Ax.Z)), () => IsMainTabOpen)
                .CreateAnyKeyUpCommand(stopAsync, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.E, () =>
                {
                    if (_laserMachine.GetValveState(Valves.Light))
                    {
                        _laserMachine.SwitchOffValve(Valves.Light);
                    }
                    else
                    {
                        _laserMachine.SwitchOnValve(Valves.Light);
                    }
                    return Task.CompletedTask;
                }, () => true)
                .CreateKeyDownCommand(Key.G, () => _laserMachine.GoThereAsync(LMPlace.Loading), () => IsMainTabOpen)
                .CreateKeyDownCommand(Key.Home, async () =>
                {
                    await moveHomeAsync();
                    _signalColumn.TurnOnLight(LightColumn.Light.Green);
                    Growl.Clear();
                }, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Add, changeVelocity, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Subtract, setStepVelocity, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Q, () =>
                {
                    ChangeViews();
                    return Task.CompletedTask;
                }, () => IsMainTabOpen)
                .CreateKeyDownCommand(Key.W, () =>
                {
                    ChangeMechView();
                    return Task.CompletedTask;
                }, () => IsMainTabOpen)
                .CreateKeyDownCommand(Key.Multiply, next, () => !IsProcessing)
                .CreateKeyDownCommand(Key.Escape, deny, () => true)
                .CreateKeyDownCommand(Key.P, async () =>
                {
                    await Task.WhenAll(
                        _laserMachine.MoveAxInPosAsync(Ax.X, TestX, true),
                        _laserMachine.MoveAxInPosAsync(Ax.Y, TestY, true)
                        );
                }, () => false)
                .CreateKeyDownCommand(Key.L, () =>
                {
                    //Dialog.Show<MaterialDialogControl>()
                    //.SetDataContext<AskLearnFocus>()
                    //.Initialize<AskLearnFocus>(vm =>
                    //{
                    //    vm.Height = 15;
                    //    vm.Speed = 1000;
                    //})
                    //.GetResultAsync<AskLearnFocus>()
                    //.ContinueWith(result =>
                    //{
                    //    //    var w = result.Result;

                    //    //        MarkSpeed=10000,
                    //    //        PWMFrequency=100,
                    //    //        PWMDutyCycle=90,
                    //    //        QPulseWidth=1,
                    //    //        PowerRatio=50
                    //    //    }));
                    //    //    double xOffset = Settings.Default.XOffset;
                    //    //    double yOffset = Settings.Default.YOffset;
                    //    //    await Task.WhenAll(
                    //    //       _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { xOffset, yOffset }, true),
                    //    //       _laserMachine.MoveAxInPosAsync(Ax.Z, Settings.Default.ZeroPiercePoint - WaferThickness)
                    //    //       );
                    //    //    await _laserMachine.MoveAxRelativeAsync(Ax.Z, -10);
                    //    //    for (int i = 1; i < 41; i++)
                    //    ////    _laserMachine.SetExtMarkParams(new MachineClassLibrary.Laser.ExtParamsAdapter(new()
                    //    ////    {
                    //    //    {
                    //    //        await _laserMachine.PierceLineAsync(0, -5, 0, 5);
                    //    //        await _laserMachine.MoveAxRelativeAsync(Ax.X, 0.2);
                    //    //        await _laserMachine.MoveAxRelativeAsync(Ax.Z, 0.2);
                    //    ////        Freq = 40000,
                    //    //    }

                    //    //    await Task.WhenAll(
                    //    //      _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { -xOffset, -yOffset }, true),
                    //    //      _laserMachine.MoveAxInPosAsync(Ax.Z, Settings.Default.ZeroFocusPoint - WaferThickness)
                    //    //      );
                    //});

                    //Dialog.Show<AskThicknessDialog>()
                    //.SetDataContext<AskThicknessVM>()
                    //.Initialize<AskThicknessVM>(vm =>
                    //{
                    //    vm.Thickness = 0.5;
                    //})
                    //.GetResultAsync<AskThicknessVM>()
                    //.ContinueWith(result =>
                    //{
                    //    var t = result.Result.Thickness;
                    //});

                    Growl.Ask("WTF?", s =>
                    {
                        return true;
                    });

                    return Task.CompletedTask;
                }, () => false)
                .CreateKeyDownCommand(Key.F7, () =>
                {
                    _laserMachine.InvokeSettings();
                    return Task.CompletedTask;
                }, () => false);

            async Task moveAxDirAsync((Ax, AxDir) axDir)
            {
                if (VelocityRegime != Velocity.Step) _laserMachine.GoWhile(axDir.Item1, axDir.Item2);
                if (VelocityRegime == Velocity.Step)
                {
                    var step = (axDir.Item2 == AxDir.Pos ? 1 : -1) * 0.005;
                    await _laserMachine.MoveAxRelativeAsync(axDir.Item1, step, false);

                }
            }


            async Task moveAsync(KeyEventArgs key)
            {
                try
                {
                    var res = key.Key switch
                    {
                        Key.A => (Ax.Y, AxDir.Pos),
                        Key.Z => (Ax.Y, AxDir.Neg),
                        Key.X => (Ax.X, AxDir.Neg),
                        Key.C => (Ax.X, AxDir.Pos),
                        Key.V => (Ax.Z, AxDir.Pos),
                        Key.B => (Ax.Z, AxDir.Neg),
                    };

                    if (!key.IsRepeat)
                    {
                        if (VelocityRegime != Velocity.Step) _laserMachine.GoWhile(res.Item1, res.Item2);
                        if (VelocityRegime == Velocity.Step)
                        {
                            var step = (res.Item2 == AxDir.Pos ? 1 : -1) * 0.005;
                            await _laserMachine.MoveAxRelativeAsync(res.Item1, step, false);

                        }
                    }
                    key.Handled = true;
                }
                catch (SwitchExpressionException ex)
                {
                    _logger.ForContext<MainViewModel>().Error(ex, $"Swallowed the exception in the {nameof(moveAsync)} method.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the {nameof(moveAsync)} method.");
                    throw;
                }
            }
            Task stopAsync(KeyEventArgs key)
            {
                try
                {
                    var axis = key.Key switch
                    {
                        Key.A or Key.Z => Ax.Y,
                        Key.X or Key.C => Ax.X,
                        Key.V or Key.B => Ax.Z,
                        _ => Ax.None
                    };
                    if (axis != Ax.None) _laserMachine.Stop(axis);
                }
                catch (SwitchExpressionException ex)
                {
                    _logger.ForContext<MainViewModel>().Error(ex, $"Swallowed the exception in the {nameof(stopAsync)} method.");
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the {nameof(stopAsync)} method.");
                    throw;
                }
                return Task.CompletedTask;
            }
            async Task moveHomeAsync()
            {
                try
                {
                    await _laserMachine.GoHomeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the {nameof(moveHomeAsync)} method.");
                    throw;
                }
                finally
                {
                    VelocityRegime = Velocity.Fast;
                    _laserMachine.SetVelocity(VelocityRegime);
                }
            }
            Task changeVelocity()
            {
                VelocityRegime = VelocityRegime switch
                {
                    Velocity.Slow => Velocity.Fast,
                    Velocity.Fast => Velocity.Slow,
                    _ => Velocity.Fast
                };
#if PCIInserted
                _laserMachine.SetVelocity(VelocityRegime);
#endif
                return Task.CompletedTask;
            }
            Task setStepVelocity()
            {
                VelocityRegime = Velocity.Step;
#if PCIInserted
                _laserMachine.SetVelocity(Velocity.Slow);
#endif
                return Task.CompletedTask;
            }
            Task next()
            {
                if (_canTeach)
                {
                    Growl.Clear();
                    return _currentTeacher?.Next();
                }
                return _mainProcess?.Next();
            }
            Task deny()
            {
                if (_canTeach)
                {
                    Growl.Clear();
                    return _currentTeacher?.Deny();
                }
                return Task.CompletedTask;
            }
        }
        [ICommand]
        private async Task OpenFileViewSettingsWindow()
        {
            //var materialResponse = await _mediator.Send(new GetFullMaterialRequest());
            var defLayerResponse = await _mediator.Send(new GetDefaultLayerFiltersFullRequest());
            //var defEntTechResponse = await _mediator.Send(new GetFullDefaultLayerEntityTechnologyRequest());

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Отображение слоёв и технологии")
                .SetDataContext<FileViewDialogVM>(vm =>
                {
                    vm.DefLayerFilters = defLayerResponse.DefaultLayerFilters.ToObservableCollection();
                    //vm.DefaultTechnologies = defEntTechResponse.DefaultLayerEntityTechnologies.ToObservableCollection();
                    //vm.Materials = materialResponse.Materials.ToObservableCollection();
                    vm.DefaultHeight = _settingsManager.Settings.DefaultHeight ?? throw new ArgumentNullException("DefaultHeight is null");
                    vm.DefaultWidth = _settingsManager.Settings.DefaultWidth ?? throw new ArgumentNullException("DefaultWidth is null");
                    vm.IsMirrored = _settingsManager.Settings.IsMirrored ?? throw new ArgumentNullException("IsMirrored is null");
                    vm.IsRotated = _settingsManager.Settings.WaferAngle90 ?? throw new ArgumentNullException("WaferAngle90 is null");
                })
                .GetCommonResultAsync<FileViewDialogVM>();//UNDONE this dialog doesn't save db
            if (result.Success)
            {
                _settingsManager.Settings.DefaultWidth = result.CommonResult.DefaultWidth;
                _settingsManager.Settings.DefaultHeight = result.CommonResult.DefaultHeight;
                _settingsManager.Settings.IsMirrored = result.CommonResult.IsMirrored;
                _settingsManager.Settings.WaferAngle90 = result.CommonResult.IsRotated;
                _settingsManager.Save();

                var newFilters = result.CommonResult.DefLayerFilters.Where(f => f.Id == 0).ToList();
                var deletedFilters = defLayerResponse.DefaultLayerFilters.Except(result.CommonResult.DefLayerFilters).ToList();
                if (newFilters.Any()) await _mediator.Send(new CreateDefaultLayerFiltersRequest(newFilters));
                if (deletedFilters.Any()) await _mediator.Send(new DeleteDefaultLayerFiltersRequest(deletedFilters));
            }
        }
        [ICommand]
        private async Task OpenSpecimenSettingsWindow()
        {
            var response = await _mediator.Send(new GetFullDefaultLayerEntityTechnologyRequest());
            var defLayerEntTechnologies = response.DefaultLayerEntityTechnologies;
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Ориентация и технология по-умолчанию")
                .SetDataContext<SpecimenSettingsVM>(vm =>
                {
                    vm.DefaultTechSelectors = defLayerEntTechnologies
                    .GroupBy(d => d.DefaultLayerFilter, (k, col) =>
                      new DefaultTechSelector(k, col.GroupBy(g => g.EntityType)
                      .ToImmutableDictionary(k => k.Key, e => e.Select(g => g.Technology.Material))
                      )).ToObservableCollection();
                    var defLayerProcDTO = ExtensionMethods.DeserilizeObject<DefaultProcessFilterDTO>(AppPaths.DefaultProcessFilter);
                    if (defLayerProcDTO is not null)
                    {
                        vm.DefaultHeight = defLayerProcDTO.DefaultHeight;
                        vm.DefaultWidth = defLayerProcDTO.DefaultWidth;

                        var defsel = vm.DefaultTechSelectors?.SingleOrDefault(d => d.DefLayerFilter.Id == defLayerProcDTO.LayerFilterId);
                        var defType = (LaserEntity)defLayerProcDTO.EntityType;

                        if (defsel?.Entities?.Contains(defType) ?? false)
                        {
                            if (defsel.EntMaterials.TryGetValue(defType, out var materials))
                            {
                                var defmaterial = materials.SingleOrDefault(m => m.Id == defLayerProcDTO.MaterialId);
                                if (defmaterial is not null)
                                {
                                    vm.DefaultTechSelector = defsel;
                                    vm.DefaultEntityType = defType;
                                    vm.DefaultMaterial = defmaterial;
                                }
                            }
                        }
                    }
                    vm.IsMirrored = _settingsManager.Settings.WaferMirrorX ?? throw new ArgumentNullException("WaferMirrorX is null");
                    vm.IsRotated = _settingsManager.Settings.WaferAngle90 ?? throw new ArgumentNullException("WaferAngle90 is null");
                })
                .GetCommonResultAsync<SpecimenSettingsVM>();
            if (result.Success)
            {
                var defSettings = result.CommonResult;
                var defProcFilter = new DefaultProcessFilterDTO
                {
                    LayerFilterId = defSettings.DefaultTechSelector.DefLayerFilter.Id,
                    MaterialId = defSettings.DefaultMaterial.Id,
                    EntityType = (uint)defSettings.DefaultEntityType,
                    DefaultWidth = defSettings.DefaultWidth,
                    DefaultHeight = defSettings.DefaultHeight
                };

                defProcFilter.SerializeObject(AppPaths.DefaultProcessFilter);

                _settingsManager.Settings.WaferMirrorX = defSettings.IsMirrored;
                _settingsManager.Settings.WaferAngle90 = defSettings.IsRotated;
                _settingsManager.Save();
            }
        }
        [ICommand]
        private async Task MachineSettings()
        {
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Настройки приводов")
                .SetDataContext(new MachineSettingsVM(XAxis.Position, YAxis.Position, ZAxis.Position), vm => vm.CopyFromSettings2(_settingsManager.Settings))
                .GetCommonResultAsync<MachineSettingsVM>();
            if (result.Success)
            {
                result.CommonResult.CopyToSettings2(_settingsManager.Settings);
                _settingsManager.Save();
                ImplementMachineSettings();
                TuneCoorSystem();
            }
        }
        [ICommand]
        private async Task ChooseMaterial()
        {
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Подложка")
                .SetDataContext<WaferVM>(vm =>
                {
                    vm.Width = WaferWidth;
                    vm.Height = WaferHeight;
                    vm.Thickness = WaferThickness;
                })
                .GetCommonResultAsync<WaferVM>();
            if (result.Success)
            {
                WaferWidth = result.CommonResult.Width;
                WaferHeight = result.CommonResult.Height;
                WaferThickness = result.CommonResult.Thickness;
                _settingsManager.Settings.WaferWidth = WaferWidth;
                _settingsManager.Settings.WaferHeight = WaferHeight;
                _settingsManager.Settings.WaferThickness = WaferThickness;
                _settingsManager.Save();

            }
        }
        [ICommand]
        private async Task OpenPenHatchSettings()
        {
            var msVM = _serviceProvider.GetService<MarkSettingsVM>();

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Настройка пера и штриховки")
                .SetDataContext(msVM, vm => { })
                .GetCommonResultAsync<MarkSettingsVM>();

            if (result.Success)
            {
                var defLaserParams = result.CommonResult.GetLaserParams();
                defLaserParams.SerializeObject(AppPaths.DefaultLaserParams);
            }
        }
        [ICommand]
        private async Task GesturePressed(Compass direction)
        {
            if (IsProcessing) return;
            var coordinates = direction switch
            {
                Compass.NE => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, WaferWidth, WaferHeight),
                Compass.NW => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, 0, WaferHeight),
                Compass.SW => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, 0, 0),
                Compass.SE => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, WaferWidth, 0),
                _ => null
            };
            if (coordinates is null)
            {
                var xy = _coorSystem.FromSub(LMPlace.FileOnWaferUnderCamera, XAxis.Position, YAxis.Position);
                var x = xy[0];
                var y = xy[1];
                coordinates = direction switch
                {
                    Compass.W => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, 0, y),
                    Compass.E => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, WaferWidth, y),
                    Compass.N => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, x, WaferHeight),
                    Compass.S => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, x, 0),
                    Compass.CenterV => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, x, WaferHeight / 2),
                    Compass.CenterH => _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, WaferWidth / 2, y),
                    _ => null
                };
            }
            if (coordinates != null)
            {
                var velTemp = _laserMachine.VelocityRegime;
                _laserMachine.SetVelocity(Velocity.Service);
                await Task.WhenAll(
                      _laserMachine.MoveAxInPosAsync(Ax.X, coordinates[0]),
                      _laserMachine.MoveAxInPosAsync(Ax.Y, coordinates[1])
                  );
                _laserMachine.SetVelocity(velTemp);
            }
        }
        [ICommand]
        private void CameraCapabilitiesChanged()
        {
            _laserMachine.StopCamera();
            _laserMachine.StartCamera(0, CameraCapabilitiesIndex);
            _settingsManager.Settings.PreferredCameraCapabilities = CameraCapabilitiesIndex;
            _settingsManager.Save();
        }

        [ICommand]
        private async Task OpenMarkSettings()
        {
            try
            {
                var markParams = ExtensionMethods
                                    .DeserilizeObject<ExtendedParams>(AppPaths.MarkTextParams);


                var result = await Dialog.Show<CommonDialog>()
                    .SetDialogTitle("Параметры пера")
                    .SetDataContext(new EditExtendedParamsVM(markParams), vm => { })
                    .GetCommonResultAsync<ExtendedParams>();

                if (result.Success)
                {
                    result.CommonResult.SerializeObject(AppPaths.MarkTextParams);
                }
            }
            catch (Exception ex)
            {
                _logger.ForContext<MainViewModel>().Error(ex, $"Swallowed the exception in the {nameof(OpenMarkSettings)} method.");
            }
        }
    }
    public enum Compass
    {
        N,
        S,
        E,
        W,
        NE,
        NW,
        SE,
        SW,
        CenterV,
        CenterH
    }
}

