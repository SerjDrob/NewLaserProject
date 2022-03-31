using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NewLaserProject.ViewModels;

internal partial class MainViewModel
{
    [ICommand]
    private void LeftWaferCornerTeach()
    {
        var lwct = WaferCornerTeacher.GetBuilder();

        lwct.SetOnRequestPermissionToStartAction(() => Task.Run(async () =>
        {
            if (MessageBox.Show("Обучить левую точку пластины?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _currentTeacher.Accept();
            }
            else
            {
                await _currentTeacher.Deny();
            }
        }))
        .SetOnGoCornerPointAction(async () =>
        {
            var x = Settings.Default.XLeftPoint;
            var y = Settings.Default.YLeftPoint;
            var z = Settings.Default.ZObjective;

            // await Task.WhenAll(new Task[]
            //{
            await _laserMachine.MoveGpInPosAsync(Groups.XY, new double[] { x, y }, false);
            await _laserMachine.MoveAxInPosAsync(Ax.Z, z);
            //});
            //MessageBox.Show("Наведите перекрестие на угол и нажмите * чтобы продолжить", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
            techMessager.RealeaseMessage("Наведите перекрестие на угол и нажмите * чтобы продолжить", Icon.Exclamation);
        })
        .SetOnRequestPermissionToAcceptAction(() => Task.Run(async () =>
        {
            techMessager.EraseMessage();
            var x = XAxis.Position;
            var y = YAxis.Position;
            _currentTeacher.SetParams(new double[] { x, y });

            if (MessageBox.Show($"Принять новые координаты точки {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _currentTeacher.Accept();
            }
            else
            {
                await _currentTeacher.Deny();
            }
        }))
        .SetOnHasResultAction(() => Task.Run(() =>
        {
            Settings.Default.XLeftPoint = _currentTeacher.GetParams()[0];
            Settings.Default.YLeftPoint = _currentTeacher.GetParams()[1];
            Settings.Default.Save();
            //MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
            techMessager.RealeaseMessage($"Новое значение {_currentTeacher} установленно", Icon.Info);
            _canTeach = false;
        }))
        .SetOnCornerToughtAction(() => Task.Run(() =>
        {
            //MessageBox.Show("Обучение отменено", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
            techMessager.RealeaseMessage("Обучение отменено", Icon.Exclamation);
            _canTeach = false;
        }));
        _currentTeacher = lwct.Build();
        _canTeach = true;
    }
    [ICommand]
    private void RightWaferCornerTeach() { }//WaferAligningTeacher
    [ICommand]
    private async Task TeachCameraOffset()
    {
        //if(_canTeach) return;
        var teachPosition = new double[] { 1, 1 };
        double xOffset = 10;
        double yOffset = 10;


        var tcb = CameraOffsetTeacher.GetBuilder();
        tcb.SetOnGoLoadPointAction(() => Task.Run(async () =>
        {
            await _laserMachine.GoThereAsync(LMPlace.Loading);
            techMessager.RealeaseMessage("Установите подложку и нажмите * чтобы продолжить", Icon.Info);
        }))
            .SetOnGoUnderCameraAction(() => Task.Run(async () =>
            {
                await _laserMachine.MoveGpInPosAsync(Groups.XY, teachPosition);
                techMessager.RealeaseMessage("Выбирете место прожига и нажмите * чтобы продолжить", Icon.Info);
            }))
            .SetOnGoToShotAction(() => Task.Run(async () =>
            {
                techMessager.EraseMessage();
                await _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { xOffset, yOffset }, true);
                //await _laserMachine.PiercePointAsync();
                _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                await _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { -xOffset, -yOffset }, true);
                await _currentTeacher.Accept();
            }))
            .SetOnSearchScorchAction(() =>
            {
                techMessager.RealeaseMessage("Совместите место прожига с перекрестием камеры и нажмите * чтобы продолжить", Icon.Info);
                return Task.CompletedTask;
            })
            .SetOnRequestPermissionToStartAction(() => Task.Run(async () =>
            {
                if (MessageBox.Show("Обучить смещение камеры от объектива лазера?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _currentTeacher.Accept();
                }
                else
                {
                    await _currentTeacher.Deny();
                }
            }))
            .SetOnRequestPermissionToAcceptAction(() => Task.Run(async () =>
            {
                _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                if (MessageBox.Show($"Принять новое смещение камеры от объектива лазера {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _currentTeacher.Accept();
                }
                else
                {
                    await _currentTeacher.Deny();
                }
            }))
            .SetOnBiasToughtAction(() => Task.Run(() =>
            {
                techMessager.RealeaseMessage("Обучение отменено", Icon.Exclamation);
                _canTeach = false;
            }))
            .SetOnHasResultAction(() => Task.Run(() =>
            {
                Settings.Default.XOffset = _currentTeacher.GetParams()[0];
                Settings.Default.YOffset = _currentTeacher.GetParams()[1];
                Settings.Default.Save();
                techMessager.RealeaseMessage("Новое значение установленно", Icon.Exclamation);
                _canTeach = false;
            }));
        _currentTeacher = tcb.Build();
        await _currentTeacher.StartTeach();
        _canTeach = true;
    }
    [ICommand]
    private void TeachScanatorHorizont()
    {
        var waferWidth = 60;
        var delta = 5;
        var xLeft = delta;
        var xRight = waferWidth - delta;
        var waferHeight = 48;
        float tempX = 0;

        var tcb = LaserHorizontTeacher.GetBuilder();

        tcb.SetGoUnderCameraAction(async () =>
        {
            await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(waferWidth / 2, waferHeight / 2));
            VideoScreenMessage = "Выберете место на пластине для прожига горизонтальной линии";
        })
            .SetGoAtFirstPointAction(() => Task.Run(async () =>
            {
                await _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { Settings.Default.XOffset, Settings.Default.YOffset }, true);
                var matrix = new System.Drawing.Drawing2D.Matrix();
                matrix.Rotate((float)Settings.Default.PazAngle);
                var points = new PointF[] { new PointF(xLeft - waferWidth / 2, 0), new PointF(xRight - waferWidth / 2, 0) };
                matrix.TransformPoints(points);
                tempX = points[0].X;
                await _laserMachine.PierceLineAsync(-waferWidth / 2, 0, waferWidth / 2, 0);
                await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(tempX, waferHeight / 2));
                VideoScreenMessage = "Установите перекрестие на первую точку линии и нажмите *";
                tempX = points[1].X;
            }))
            .SetGoAtSecondPointAction(() => Task.Run(async () =>
            {
                _currentTeacher.SetParams(new double[] { XAxis.Position, YAxis.Position });
                await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(tempX, waferHeight / 2));
                VideoScreenMessage = "Установите перекрестие на вторую точку линии и нажмите *";
            }))
            .SetOnRequestPermissionToStartAction(() => Task.Run(() =>
            {
                if (MessageBox.Show("Обучить горизонтальность сканатора?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _currentTeacher.Accept();
                }
                else
                {
                    _currentTeacher.Deny();
                }
            }))
            .SetOnRequestPermissionToAcceptAction(() => Task.Run(() =>
            {
                _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                if (MessageBox.Show($"Принять новое значение горизонтальности сканатора {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _currentTeacher.Accept();
                }
                else
                {
                    _currentTeacher.Deny();
                }
            }))
            .SetOnLaserHorizontToughtAction(() => Task.Run(() =>
            {
                MessageBox.Show("Обучение отменено", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                _canTeach = false;
            }))
            .SetOnHasResultAction(() => Task.Run(() =>
            {
                var result = _currentTeacher.GetParams();
                var point1 = new PointF((float)result[0], (float)result[1]);
                var point2 = new PointF((float)result[2], (float)result[3]);
                double AC = point2.X - point1.X;
                double CB = point2.Y - point1.Y;
                Settings.Default.PazAngle = Math.Atan2(CB, AC);
                Settings.Default.Save();
                MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                _canTeach = false;
            }));
        _currentTeacher = tcb.Build();
        _canTeach = true;
    }
    [ICommand]
    private void TeachOrthXY(List<PointF> points)
    {
        Guard.IsEqualTo(points.Count, 3, nameof(points));
        using var pointsEnumerator = points.GetEnumerator();
        var tcb = XYOrthTeacher.GetBuilder();
        tcb.SetOnGoNextPointAction(() => Task.Run(async () =>
        {
            pointsEnumerator.MoveNext();
            var point = pointsEnumerator.Current;
            await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(point.X, point.Y), true);
        }))
            .SetOnRequestPermissionToStartAction(() => Task.Run(() =>
            {
                if (MessageBox.Show("Обучить координатную систему лазера?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _currentTeacher.Accept();
                }
                else
                {
                    _currentTeacher.Deny();
                }
            }))
            .SetOnRequestPermissionToAcceptAction(() => Task.Run(() =>
            {
                _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                if (MessageBox.Show($"Принять новое значения координат для матрицы преобразования {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _currentTeacher.Accept();
                }
                else
                {
                    _currentTeacher.Deny();
                }
            }))
            .SetOnXYOrthToughtAction(() => Task.Run(() =>
            {
                MessageBox.Show("Обучение отменено", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                _canTeach = false;
            }))
            .SetOnHasResultAction(() => Task.Run(() =>
            {
                var resultPoints = _currentTeacher.GetParams();
                _coorSystem = new CoorSystem<LMPlace>(
                    first: (new(points[0].X, points[0].Y), new((float)resultPoints[0], (float)resultPoints[1])),
                    second: (new(points[1].X, points[1].Y), new((float)resultPoints[2], (float)resultPoints[3])),
                    third: (new(points[2].X, points[2].Y), new((float)resultPoints[4], (float)resultPoints[5])));
                TuneCoorSystem(_coorSystem);
                _coorSystem.SerializeObject("filePath");
                MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                _canTeach = false;
            }));
        _currentTeacher = tcb.Build();
        _canTeach = true;
    }
    [ICommand]
    private async Task TeachCameraScale()
    {
        var teachPosition = new double[] { 1, 1 };

        var tcs = CameraScaleTeacher.GetBuilder()
            .SetOnRequestPermissionToStartAction(() => Task.Run(async () =>
            {
                if (MessageBox.Show("Обучить масштаб видео?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _currentTeacher.Accept();
                }
                else
                {
                    await _currentTeacher.Deny();
                }
            }))
            .SetOnGoLoadPointAction(() => Task.Run(async () =>
            {
                await _laserMachine.GoThereAsync(LMPlace.Loading);
                techMessager.RealeaseMessage("Установите подложку и нажмите * чтобы продолжить", Icon.Info);
            }))
            .SetOnGoNAskFirstMarkerAction(() => Task.Run(async () =>
            {
                await _laserMachine.MoveGpInPosAsync(Groups.XY, teachPosition);
                TeachScaleMarkerEnable = true;
                techMessager.RealeaseMessage("Подведите один из маркеров к ориентиру и нажмите * чтобы продолжить", Icon.Info);
            }))
            .SetOnAskSecondMarkerAction(() => Task.Run(async () =>
            {
                _currentTeacher.SetParams(new double[] { YAxis.Position });
                techMessager.RealeaseMessage("Подведите второй маркер к этому ориентиру и нажмите * чтобы продолжить", Icon.Info);
            }))
            .SetOnRequestPermissionToAcceptAction(() => Task.Run(async () =>
            {
                TeachScaleMarkerEnable = false;
                var diff = Math.Abs(YAxis.Position - _currentTeacher.GetParams()[0]);
                var scale = diff / (ScaleMarkersRatioSecond - ScaleMarkersRatioFirst);
                if (MessageBox.Show($"Принять новый масштаб видео  {scale}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _currentTeacher.SetParams(new double[] { scale });
                    await _currentTeacher.Accept();
                }
                else
                {
                    await _currentTeacher.Deny();
                }

            }))
            .SetOnScaleToughtAction(() => Task.Run(async () =>
            {
                techMessager.RealeaseMessage("Обучение отменено", Icon.Exclamation);
                _canTeach = false;
            }))
            .SetOnHasResultAction(() => Task.Run(async () =>
            {
                Settings.Default.CameraScale = _currentTeacher.GetParams()[0];
                Settings.Default.Save();
                techMessager.RealeaseMessage("Новое значение установленно", Icon.Exclamation);
                _canTeach = false;
            }));
            _currentTeacher = tcs.Build();
        await _currentTeacher.StartTeach();
        _canTeach = true;

    }
    [ICommand]
    private Task TeachNext()
    {
        if (_canTeach)
        {
            return _currentTeacher?.Next();
        }
        return null;
    }
    [ICommand]
    private Task TeachDeny()
    {
        if (_canTeach)
        {
            return _currentTeacher?.Deny();
        }
        return null;
    }
}
