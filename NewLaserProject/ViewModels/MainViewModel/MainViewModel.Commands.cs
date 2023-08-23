﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
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

            TestKeyCommand = new KeyProcessorCommands(parameter => _notPreventingKeyProcessing)
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
                }, () => true)
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
                }, () => true)
                .CreateKeyDownCommand(Key.F7, () =>
                {
                    _laserMachine.InvokeSettings();
                    return Task.CompletedTask;
                }, () => true);

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
                catch (SwitchExpressionException)
                {
                    return;
                }
                catch (Exception)
                {
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
                catch (SwitchExpressionException)
                {
                    return Task.CompletedTask;
                }
                catch (Exception)
                {
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
                    throw;
                }
                techMessager.EraseMessage();
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
                return null;
            }
        }
        private bool _notPreventingKeyProcessing = true;
        [ICommand]
        private void OpenFileViewSettingsWindow()
        {
            _notPreventingKeyProcessing = false;

            var dialog = Dialog.Show<CommonDialog>()
                .SetDialogTitle("Фильтры слоёв файла")
                .SetDataContext<FileViewDialogVM>(vm => vm.DefLayerFilters = _db.Set<DefaultLayerFilter>().Local.ToObservableCollection())
                .GetCommonResultAsync<IEnumerable<DefaultLayerFilter>>()
                .ContinueWith(t =>
                {
                    if(t.Result.Success) _db.SaveChanges();
                    _notPreventingKeyProcessing = true;
                });
        }
        [ICommand]
        private void OpenSpecimenSettingsWindow()
        {
            _notPreventingKeyProcessing = false;

            var dialog = Dialog.Show<CommonDialog>()
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

                            if (defsel is not null && defsel.Entities.Contains(defType))
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
                .GetCommonResultAsync<SpecimenSettingsVM>()
                .ContinueWith(t =>
                {
                    var result = t.Result;
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

                        defProcFilter.SerializeObject(ProjectPath.GetFilePathInFolder(APP_SETTINGS_FOLDER, "DefaultProcessFilter.json"));

                        Settings.Default.WaferMirrorX = defSettings.IsMirrored;
                        Settings.Default.WaferAngle90 = defSettings.IsRotated;
                        Settings.Default.Save();
                    }
                    _notPreventingKeyProcessing = true;
                });
        }
        [ICommand]
        private void MachineSettings()
        {
            _notPreventingKeyProcessing = false;
            Dialog.Show<CommonDialog>()
                .SetDialogTitle("Настройки приводов")
                .SetDataContext(new MachineSettingsVM(XAxis.Position, YAxis.Position, ZAxis.Position), vm => vm.CopyFromSettings())
                .GetCommonResultAsync<MachineSettingsVM>()
                .ContinueWith(t =>
                {
                    var result = t.Result;
                    if (result.Success)
                    {
                        result.CommonResult.CopyToSettings();
                        Settings.Default.Save();
                        ImplementMachineSettings();
                    }                    
                    _notPreventingKeyProcessing = true;
                });
        }

        [ICommand]
        private void ChooseMaterial()
        {
            _notPreventingKeyProcessing = false;
            Dialog.Show<CommonDialog>()
                .SetDialogTitle("Подложка")
                .SetDataContext<MaterialVM>(vm =>
                {
                    vm.Width = WaferWidth;
                    vm.Height = WaferHeight;
                    vm.Thickness = WaferThickness;
                })
                .GetCommonResultAsync<MaterialVM>()
                .ContinueWith(t=> {
                    var result = t.Result;
                    if (result.Success)
                    {
                        WaferWidth = result.CommonResult.Width;
                        WaferHeight = result.CommonResult.Height;
                        WaferThickness = result.CommonResult.Thickness;
                    }                   
                    _notPreventingKeyProcessing = true;
                });
        }
        [ICommand]
        private void OpenPenHatchSettings()
        {
            var defLaserParams = ExtensionMethods.DeserilizeObject<MarkLaserParams>(ProjectPath.GetFilePathInFolder(APP_SETTINGS_FOLDER, "DefaultLaserParams.json"));
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MarkLaserParams, MarkSettingsVM>()
                .IncludeMembers(s => s.PenParams, s => s.HatchParams);
                cfg.CreateMap<PenParams, MarkSettingsVM>(MemberList.None);
                cfg.CreateMap<HatchParams, MarkSettingsVM>(MemberList.None);
            });

            var markParamsToMSVMMapper = config.CreateMapper();
            var context = markParamsToMSVMMapper.Map<MarkSettingsVM>(defLaserParams);

            _notPreventingKeyProcessing = false;
            Dialog.Show<CommonDialog>()
                .SetDialogTitle("Настройка пера и штриховки")
                .SetDataContext(context, vm => { })
                .GetCommonResultAsync<MarkSettingsVM>()
                .ContinueWith(t => {
                    var result = t.Result;
                    if (result.Success)
                    {
                        var defLaserParams = result.CommonResult.GetLaserParams();
                        defLaserParams.SerializeObject(ProjectPath.GetFilePathInFolder(APP_SETTINGS_FOLDER, "DefaultLaserParams.json"));
                    }
                    _notPreventingKeyProcessing = true;
                });
        }
    }
}

