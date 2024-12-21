using HandyControl.Controls;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Miscellaneous;
using MachineClassLibrary.Settings;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    internal class CameraGroupOffsetTeacher : ITeacher
    {
        private readonly IEnumerable<(double, double)> _coordinates;
        private readonly LaserMachine _laserMachine;
        private readonly ISettingsManager<LaserMachineSettings> _settingsManager;
        private readonly double _waferThickness;
        private StateMachine<MyState, MyTrigger> _stateMachine;
        private double _xOffset;
        private double _yOffset;
        private List<OffsetPoint> _offsetPoints = new();


        public event EventHandler? TeachingCompleted;

        public CameraGroupOffsetTeacher(ICoorSystem coorSystem, LaserMachine laserMachine, 
            ISettingsManager<LaserMachineSettings> settingsManager, double waferThickness, double width, double height, IEnumerable<(double,double)> points)
        {
            _coordinates = points.Select(p =>
            {
                var res = coorSystem.ToGlobal(p.Item1, p.Item2);
                return (res[0], res[1]);
            }).ToList();
            _laserMachine = laserMachine;
            _settingsManager = settingsManager;
            _waferThickness = waferThickness;
            var zCamera = _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null");
            _xOffset = _settingsManager.Settings.XOffset ?? throw new ArgumentNullException("XOffset is null");
            _yOffset = _settingsManager.Settings.YOffset ?? throw new ArgumentNullException("YOffset is null");

            var enumerator = _coordinates.GetEnumerator(); // TODO it is disposable

            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);
            (double x, double y) coordinate = (0,0);
            var reentry = !enumerator.MoveNext();
            if(reentry) throw new Exception("The sequence has no elements.");


            _stateMachine.Configure(MyState.Begin)
                .OnActivate(()=>Growl.Info(""" Для начала обучения нажмите "*" """))
                .Permit(MyTrigger.Next, MyState.UnderCamera)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);


            _stateMachine.Configure(MyState.UnderCamera)
                .OnEntryAsync(async () =>
                {
                    Growl.Clear();
                    coordinate = enumerator.Current;


                    await Task.WhenAll(
                         _laserMachine.MoveAxInPosAsync(Ax.X, coordinate.x, true),
                         _laserMachine.MoveAxInPosAsync(Ax.Y, coordinate.y, true),
                         _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera - waferThickness)
                         );
                    Growl.Info(""" Совместите перекрестие и нажмите "*" """);
                    reentry = enumerator.MoveNext();
                })
                .OnExit(() =>
                {
                    _settingsManager.Settings.OffsetPoints.GetOffsetByCurCoor(coordinate.x, coordinate.y, ref _xOffset, ref _yOffset);

                    _xOffset = coordinate.x + _xOffset - _laserMachine.GetAxActual(Ax.X);
                    _yOffset = coordinate.y + _yOffset - _laserMachine.GetAxActual(Ax.Y);
                    _offsetPoints.Add(new(
                        _laserMachine.GetAxActual(Ax.X),
                        _laserMachine.GetAxActual(Ax.Y),
                        _xOffset,
                        _yOffset
                        ));
                    Growl.Info("Смещение добавлено");
                })
                .PermitReentryIf(MyTrigger.Next, () => reentry)
                .PermitIf(MyTrigger.Next, MyState.HasResult, () => !reentry)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Accept);

            _stateMachine.Configure(MyState.HasResult)
                .OnEntry(() => { 
                    _settingsManager.Settings.OffsetPoints = _offsetPoints;
                    TeachingCompleted?.Invoke(this, EventArgs.Empty); 
                })
                .OnEntry(() =>
                {
                    _settingsManager.Save();
                });

            _stateMachine.Configure(MyState.End)
                .OnEntry(() => { 
                    TeachingCompleted?.Invoke(this, EventArgs.Empty); 
                });
        }

        public Task AcceptAsync()
        {
            throw new NotImplementedException();
        }

        public Task DenyAsync() => _stateMachine.FireAsync(MyTrigger.Deny);
        

        public double[] GetParams()
        {
            throw new NotImplementedException();
        }

        public async Task NextAsync()
        {
            await _stateMachine.FireAsync(MyTrigger.Next);
        }

        public void SetParams(params double[] ps)
        {
            throw new NotImplementedException();
        }

        public void SetResult(double result)
        {
            throw new NotImplementedException();
        }
        private void GetPropOffsets(ref double xOffset, ref double yOffset)
        {
            var curX = _laserMachine.GetAxActual(Ax.X);
            var curY = _laserMachine.GetAxActual(Ax.Y);

            try
            {
                var sortResult = _settingsManager.Settings.OffsetPoints
                                .OrderBy(x => Math.Abs(x.X - curX))
                                .ThenBy(y => Math.Abs(y.Y - curY))
                                .First();
                if (sortResult != null)
                {
                    xOffset = sortResult.dx;
                    yOffset = sortResult.dy;
                }
            }
            catch (Exception) { }
        }
        public async Task StartTeachAsync()
        {
            Growl.Clear();
            _laserMachine.SetVelocity(Velocity.Service);
            var zCamera = _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null");
            var zLaser = _settingsManager.Settings.ZeroPiercePoint ?? throw new ArgumentNullException("ZeroPiercePoint is null");
            var defLaserParams = MiscExtensions
                      .DeserializeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);
            var pen = defLaserParams.PenParams with
            {
                Freq = 50000,
                MarkSpeed = 100
            };
            var hatch = defLaserParams.HatchParams;
            _laserMachine.SetMarkParams(new(pen, hatch));

            foreach ((double x, double y) coordinate in _coordinates)
            {
                //continue;

                _settingsManager.Settings.OffsetPoints.GetOffsetByCurCoor(coordinate.x, coordinate.y, ref _xOffset, ref _yOffset);

                await Task.WhenAll(
                    _laserMachine.MoveAxInPosAsync(Ax.X, coordinate.x + _xOffset, true),
                    _laserMachine.MoveAxInPosAsync(Ax.Y, coordinate.y + _yOffset, true),
                    _laserMachine.MoveAxInPosAsync(Ax.Z, zLaser - _waferThickness)
                    );

                for (int i = 0; i < 2; i++)
                {
                    await _laserMachine.PierceLineAsync(-0.5, 0, 0.5, 0);
                    await _laserMachine.PierceLineAsync(0, -0.5, 0, 0.5);
                }

                await _laserMachine.PierceLineAsync(-0.4, 0.4, 0.4, 0.4);
                await _laserMachine.PierceLineAsync(-0.4, -0.4, 0.4, -0.4);
                await _laserMachine.PierceLineAsync(-0.4, -0.4, -0.4, 0.4);
                await _laserMachine.PierceLineAsync(0.4, -0.4, 0.4, 0.4);
            }
            await _stateMachine.ActivateAsync();
        }
        private enum MyState
        {
            Begin,
            AtLoadPoint,
            UnderCamera,
            GoShot,
            AfterShot,
            RequestPermission,
            End,
            HasResult
        }
        private enum MyTrigger
        {
            Next,
            Accept,
            Deny
        }
    }
}
