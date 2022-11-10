using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using NewLaserProject.Properties;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NewLaserProject.ViewModels
{
    internal partial class MainViewModel
    {
        //public ICommand? TestKeyCommand { get; protected set; }
        public ICommand? TestKeyCommand { get; protected set; }
        private void InitCommands()
        {

            TestKeyCommand = new KeyProcessorCommands(parameter => true)
                .CreateAnyKeyDownCommand(moveAsync, () => IsMainTabOpen && !IsProcessing)
                .CreateAnyKeyUpCommand(stopAsync, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.E, () =>
                {
                    _laserMachine.SwitchOnValve(Valves.Light);
                    return Task.CompletedTask;
                }, () => true)
                .CreateKeyDownCommand(Key.G, () => _laserMachine.GoThereAsync(LMPlace.Loading), () => IsMainTabOpen)
                .CreateKeyDownCommand(Key.Home, moveHomeAsync, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Add, changeVelocity, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Subtract, setStepVelocity, () => IsMainTabOpen && !IsProcessing)
                .CreateKeyDownCommand(Key.Q, () =>
                {
                    ChangeViews();
                    return Task.CompletedTask;
                }, () => IsMainTabOpen)
                .CreateKeyDownCommand(Key.Multiply, next, () => !IsProcessing)
                .CreateKeyDownCommand(Key.Escape, deny, () => true)
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
                    var corner = new double[] { Settings.Default.XLeftPoint, Settings.Default.YLeftPoint };
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, corner).ConfigureAwait(false);
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
                    return _currentTeacher?.Next();
                }
                return _mainProcess?.Next();
            }
            Task deny()
            {
                if (_canTeach)
                {
                    return _currentTeacher?.Deny();
                }
                return null;
            }
        }
        //[ICommand]
        //private Task TeachNext()
        //{
        //    if (_canTeach)
        //    {
        //        return _currentTeacher?.Next();
        //    }
        //    return _mainProcess?.Next();
        //}
        //[ICommand]
        //private Task TeachDeny()
        //{
        //    if (_canTeach)
        //    {
        //        return _currentTeacher?.Deny();
        //    }
        //    return null;
        //}
        //[ICommand]
        //private async Task KeyDown(object args)
        //{
        //    var key = (KeyEventArgs)args;
        //    if (key.OriginalSource is TextBoxBase) return;
        //    switch (key.Key)
        //    {
        //        case Key.A or Key.Z or Key.X or Key.C or Key.V or Key.B:
        //            await moveAsync(key);
        //            break;
        //        case Key.Tab when !key.IsRepeat:
        //            await _laserMachine.MoveGpInPosAsync(Groups.XY, new double[] { 1, 1 });
        //            break;
        //        case Key.E:
        //            _laserMachine.SwitchOnValve(Valves.Light);
        //            break;
        //        case Key.G when !key.IsRepeat:
        //            await _laserMachine.GoThereAsync(LMPlace.Loading);
        //            break;
        //        case Key.J:
        //            break;
        //        case Key.K:
        //            break;
        //        case Key.L:
        //            break;
        //        case Key.Home when !key.IsRepeat:
        //            {
        //                try
        //                {
        //                    await _laserMachine.GoHomeAsync().ConfigureAwait(false);
        //                    var corner = new double[] { Settings.Default.XLeftPoint, Settings.Default.YLeftPoint };
        //                    await _laserMachine.MoveGpInPosAsync(Groups.XY, corner).ConfigureAwait(false);
        //                }
        //                catch (Exception ex)
        //                {

        //                    throw;
        //                }
        //                techMessager.EraseMessage();
        //            }
        //            break;
        //    }
        //    key.Handled = true;

        //    async Task moveAsync(KeyEventArgs key)
        //    {
        //        var res = key.Key switch
        //        {
        //            Key.A => (Ax.Y, AxDir.Pos),
        //            Key.Z => (Ax.Y, AxDir.Neg),
        //            Key.X => (Ax.X, AxDir.Neg),
        //            Key.C => (Ax.X, AxDir.Pos),
        //            Key.V => (Ax.Z, AxDir.Pos),
        //            Key.B => (Ax.Z, AxDir.Neg),
        //        };

        //        if (!key.IsRepeat)
        //        {
        //            if (VelocityRegime != Velocity.Step) _laserMachine.GoWhile(res.Item1, res.Item2);
        //            if (VelocityRegime == Velocity.Step)
        //            {
        //                var step = (res.Item2 == AxDir.Pos ? 1 : -1) * 0.005;
        //                await _laserMachine.MoveAxRelativeAsync(res.Item1, step, false);

        //            }
        //        }
        //        key.Handled = true;
        //        return;
        //    }
        //}

        //[ICommand]
        //private async Task KeyUp(object args)
        //{
        //    var key = (KeyEventArgs)args;
        //    if (key.OriginalSource is TextBoxBase) return;

        //    switch (key.Key)
        //    {
        //        case Key.Tab:
        //            break;
        //        case Key.A:
        //            _laserMachine.Stop(Ax.Y);
        //            break;
        //        case Key.B:
        //            _laserMachine.Stop(Ax.Z);
        //            break;
        //        case Key.C:
        //            _laserMachine.Stop(Ax.X);
        //            break;
        //        case Key.E:
        //            break;
        //        case Key.G:
        //            break;
        //        case Key.J:
        //            break;
        //        case Key.K:
        //            break;
        //        case Key.L:
        //            break;
        //        case Key.V:
        //            _laserMachine.Stop(Ax.Z);
        //            break;
        //        case Key.X:
        //            _laserMachine.Stop(Ax.X);
        //            break;
        //        case Key.Z:
        //            _laserMachine.Stop(Ax.Y);
        //            break;
        //    }
        //    key.Handled = true;
        //}

        //        [ICommand]
        //        private void ChangeVelocity()
        //        {
        //            VelocityRegime = VelocityRegime switch
        //            {
        //                Velocity.Slow => Velocity.Fast,
        //                Velocity.Fast => Velocity.Slow,
        //                _ => Velocity.Fast
        //            };
        //#if PCIInserted
        //            _laserMachine.SetVelocity(VelocityRegime);
        //#endif
        //        }

        //        [ICommand]
        //        private void SetStepVelocity()
        //        {
        //            VelocityRegime = Velocity.Step;
        //#if PCIInserted
        //            _laserMachine.SetVelocity(Velocity.Slow);
        //#endif
        //        }
    }
}

