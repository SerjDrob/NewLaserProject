using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DefaultLayerEntityTechnologyFeatures.Get;
using NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Get;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.Data.Models.MaterialFeatures.Create;
using NewLaserProject.Data.Models.MaterialFeatures.Get;
using NewLaserProject.Properties;
using NewLaserProject.ViewModels.DialogVM;
using NewLaserProject.Views.Dialogs;

namespace NewLaserProject.ViewModels
{
    internal partial class MainViewModel
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
                .CreateAnyKeyDownCommand(moveAsync, () => IsMainTabOpen && !IsProcessing)
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
                    Growl.Clear();
                }, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Add, changeVelocity, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Subtract, setStepVelocity, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Q, () =>
                {
                    ChangeViews();
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
                    _logger.LogError(ex, $"Swallowed the exception in the {nameof(moveAsync)} method.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Throwed the exception in the {nameof(moveAsync)} method.");
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
                    };
                    _laserMachine.Stop(axis);
                }
                catch (SwitchExpressionException ex)
                {
                    _logger.LogError(ex, $"Swallowed the exception in the {nameof(stopAsync)} method.");
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Throwed the exception in the {nameof(stopAsync)} method.");
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
                    _logger.LogError(ex, $"Throwed the exception in the {nameof(moveHomeAsync)} method.");
                    throw;
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
            var materialResponse = await _mediator.Send(new GetFullMaterialRequest());
            var defLayerResponse = await _mediator.Send(new GetDefaultLayerFiltersFullRequest());
            var defEntTechResponse = await _mediator.Send(new GetFullDefaultLayerEntityTechnologyRequest());

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Отображение слоёв и технологии")
                .SetDataContext<FileViewDialogVM>(vm =>
                {
                    vm.DefLayerFilters = defLayerResponse.DefaultLayerFilters.ToObservableCollection();
                    vm.DefaultTechnologies = defEntTechResponse.DefaultLayerEntityTechnologies.ToObservableCollection();
                    vm.Materials = materialResponse.Materials.ToObservableCollection();
                })
                .GetCommonResultAsync<IEnumerable<DefaultLayerFilter>>();
            //if (result.Success) _db.SaveChanges();//UNDONE this dialog doesn't save db
        }
        [ICommand]
        private async Task OpenSpecimenSettingsWindow()
        {
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Ориентация и технология по-умолчанию")
                .SetDataContext<SpecimenSettingsVM>(vm => _db.Set<DefaultLayerEntityTechnology>()
                    .ToArrayAsync()
                    .ContinueWith(task =>
                    {
                        vm.DefaultTechSelectors = task.Result
                        .GroupBy(d => d.DefaultLayerFilter, (k, col) =>
                          new DefaultTechSelector(k, col.GroupBy(g => g.EntityType)
                          .ToImmutableDictionary(k => k.Key, e => e.Select(g => g.Technology.Material))
                          )).ToObservableCollection();
                        var defLayerProcDTO = ExtensionMethods.DeserilizeObject<DefaultProcessFilterDTO>(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS, "DefaultProcessFilter.json"));
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
                        vm.IsMirrored = Settings.Default.WaferMirrorX;
                        vm.IsRotated = Settings.Default.WaferAngle90;
                    })
                )
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

                defProcFilter.SerializeObject(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS, "DefaultProcessFilter.json"));

                Settings.Default.WaferMirrorX = defSettings.IsMirrored;
                Settings.Default.WaferAngle90 = defSettings.IsRotated;
                Settings.Default.Save();
            }
        }
        [ICommand]
        private async Task MachineSettings()
        {
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Настройки приводов")
                .SetDataContext(new MachineSettingsVM(XAxis.Position, YAxis.Position, ZAxis.Position), vm => vm.CopyFromSettings())
                .GetCommonResultAsync<MachineSettingsVM>();
            if (result.Success)
            {
                result.CommonResult.CopyToSettings();
                Settings.Default.Save();
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
                defLaserParams.SerializeObject(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS, "DefaultLaserParams.json"));
            }
        }
        [ICommand]
        private async Task GesturePressed(Compass direction)
        {
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

