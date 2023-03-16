using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using NewLaserProject.Properties;
using NewLaserProject.UserControls;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace NewLaserProject.ViewModels
{
    internal partial class MainViewModel
    {
        public ICommand? TestKeyCommand { get; protected set; }
        public double TestX { get; set; }
        public double TestY { get; set; }
        private void InitCommands()
        {

            TestKeyCommand = new KeyProcessorCommands(parameter => true)
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
                .CreateKeyDownCommand(Key.Home, async() =>
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
                },()=>true)
                .CreateKeyDownCommand(Key.F7,()=>
                {
                    _laserMachine.InvokeSettings();
                    return Task.CompletedTask;
                },()=>true)
                ;


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
        
    }
}

