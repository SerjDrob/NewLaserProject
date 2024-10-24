using HandyControl.Controls;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using Microsoft.Toolkit.Diagnostics;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

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


        public event EventHandler TeachingCompleted;

        public CameraGroupOffsetTeacher(ICoorSystem coorSystem, LaserMachine laserMachine, 
            ISettingsManager<LaserMachineSettings> settingsManager, double waferThickness, double width, double height)
        {
            var den = new Range(6, 20);
            var ran = (3, 5);
            var cx = 5d;
            var cy = 5d;
            var gap = 3d;//mm
            var countX = Enumerable.Range(6, 20).Reverse().SkipWhile(s =>
            {
                cx = (width - gap * 2) / s;
                return !(cx >= ran.Item1 && cx <= ran.Item2);
            }).ToList()[0];
            var countY = Enumerable.Range(6, 20).Reverse().SkipWhile(s =>
            {
                cy = (height - gap * 2) / s;
                return !(cy >= ran.Item1 && cy <= ran.Item2);
            }).ToList()[0];

            var xs = Enumerable.Range(0, countX + 1).Select(x => gap + x * cx);
            var ys = Enumerable.Range(0, countY + 1).Select(y => gap + y * cy);
            _coordinates = xs.SelectMany(x => ys.Select(y =>
            {
                var res = coorSystem.ToGlobal(x, y);
                return (res[0], res[1]);
            })).ToList();
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
                         _laserMachine.MoveAxInPosAsync(Ax.X, coordinate.x - _xOffset, true),
                         _laserMachine.MoveAxInPosAsync(Ax.Y, coordinate.y - _yOffset, true),
                         _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera - waferThickness)
                         );
                    Growl.Info(""" Совместите перекрестие и нажмите "*" """);
                    reentry = enumerator.MoveNext();
                })
                .OnExit(() =>
                {
                    _xOffset = coordinate.x - _laserMachine.GetAxActual(Ax.X);
                    _yOffset = coordinate.y - _laserMachine.GetAxActual(Ax.Y);
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

        public Task Accept()
        {
            throw new NotImplementedException();
        }

        public Task Deny() => _stateMachine.FireAsync(MyTrigger.Deny);
        

        public double[] GetParams()
        {
            throw new NotImplementedException();
        }

        public async Task Next()
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
        public async Task StartTeach()
        {
            Growl.Clear();
            _laserMachine.SetVelocity(Velocity.Service);
            var zCamera = _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null");
            var zLaser = _settingsManager.Settings.ZeroPiercePoint ?? throw new ArgumentNullException("ZeroPiercePoint is null");
            var defLaserParams = ExtensionMethods
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
                await Task.WhenAll(
                    _laserMachine.MoveAxInPosAsync(Ax.X, coordinate.x, true),
                    _laserMachine.MoveAxInPosAsync(Ax.Y, coordinate.y, true),
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
    public class CameraOffsetTeacher : ITeacher
    {
        private StateMachine<MyState, MyTrigger> _stateMachine;
        private (bool init, double dx, double dy) _newOffset = (false, 0, 0);

        public event EventHandler TeachingCompleted;

        public static CameraBiasTeacherBuilder GetBuilder()
        {
            return new CameraBiasTeacherBuilder();
        }
        private CameraOffsetTeacher()
        {

        }
        private CameraOffsetTeacher(Func<Task> GoLoadPoint, Func<Task> GoUnderCamera, Func<Task> GoToSoot,
            Func<Task> OnBiasTought, Func<Task> RequestPermissionToAccept, Func<Task> RequestPermissionToStart, Func<Task> GiveResult, Func<Task> SearchScorch)
        {
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

            _stateMachine.Configure(MyState.Begin)
                .OnActivateAsync(RequestPermissionToStart)
                .Permit(MyTrigger.Accept, MyState.AtLoadPoint)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Next);


            _stateMachine.Configure(MyState.AtLoadPoint)
                .OnEntryAsync(GoLoadPoint)
                .Permit(MyTrigger.Next, MyState.UnderCamera)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.UnderCamera)
               .OnEntryAsync(GoUnderCamera)
               .Permit(MyTrigger.Next, MyState.GoShot)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.GoShot)
               .OnEntryAsync(GoToSoot, "Go under the laser, shoot and back under the camera")
               .Permit(MyTrigger.Accept, MyState.AfterShot)
               .Ignore(MyTrigger.Next)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.AfterShot)
               .OnEntryAsync(SearchScorch)
               .Permit(MyTrigger.Next, MyState.RequestPermission)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.RequestPermission)
               .OnEntryAsync(RequestPermissionToAccept)
               .Permit(MyTrigger.Accept, MyState.HasResult)
               .Permit(MyTrigger.Deny, MyState.End)
               .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.End)
               .OnEntryAsync(async () => { await OnBiasTought.Invoke(); TeachingCompleted?.Invoke(this, EventArgs.Empty); })
               .Ignore(MyTrigger.Next)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.HasResult)
                .OnEntryAsync(async () => { await GiveResult.Invoke(); TeachingCompleted?.Invoke(this,EventArgs.Empty); })
                .Ignore(MyTrigger.Next)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

        }


        public override string ToString()
        {
            return $"dx: {_newOffset.dx.ToString("0.###")}, dy: {_newOffset.dy.ToString("0.###")}";
        }
        public async Task Next() => await _stateMachine.FireAsync(MyTrigger.Next);
        public async Task Accept() => await _stateMachine.FireAsync(MyTrigger.Accept);
        public async Task Deny() => await _stateMachine.FireAsync(MyTrigger.Deny);

        public void SetParams(params double[] ps)
        {
            Guard.HasSizeEqualTo(ps, 2, nameof(ps));
            _newOffset = _newOffset.init ? (false, _newOffset.dx - ps[0], _newOffset.dy - ps[1]) : (true, ps[0], ps[1]);
        }
        //public (double dx, double dy) GetOffset() => (_newOffset.dx,_newOffset.dy);
        public double[] GetParams() => [_newOffset.dx, _newOffset.dy];

        public async Task StartTeach()
        {
            await _stateMachine.ActivateAsync();
        }

        public void SetResult(double result)
        {
            throw new NotImplementedException();
        }

        public class CameraBiasTeacherBuilder
        {
            public CameraOffsetTeacher Build()
            {
                Guard.IsNotNull(GoLoadPoint, $"{nameof(GoLoadPoint)} isn't set");
                Guard.IsNotNull(GoUnderCamera, $"{nameof(GoUnderCamera)} isn't set");
                Guard.IsNotNull(GoToSoot, $"{nameof(GoToSoot)} isn't set");
                Guard.IsNotNull(OnBiasTought, $"{nameof(OnBiasTought)} isn't set");
                Guard.IsNotNull(RequestPermissionToAccept, $"{nameof(RequestPermissionToAccept)} isn't set");
                Guard.IsNotNull(RequestPermissionToStart, $"{nameof(RequestPermissionToStart)} isn't set");
                Guard.IsNotNull(HasResult, $"{nameof(HasResult)} isn't set");
                Guard.IsNotNull(SearchScorch, $"{nameof(SearchScorch)} isn't set");
                return new CameraOffsetTeacher(GoLoadPoint, GoUnderCamera, GoToSoot, OnBiasTought, RequestPermissionToAccept, RequestPermissionToStart, HasResult, SearchScorch);
            }
            private Func<Task> GoToSoot;
            private Func<Task> GoUnderCamera;
            private Func<Task> GoLoadPoint;
            private Func<Task> OnBiasTought;
            private Func<Task> RequestPermissionToAccept;
            private Func<Task> RequestPermissionToStart;
            private Func<Task> HasResult;
            private Func<Task> SearchScorch;

            public CameraBiasTeacherBuilder SetOnGoToShotAction(Func<Task> action)
            {
                GoToSoot = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnGoUnderCameraAction(Func<Task> action)
            {
                GoUnderCamera = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnGoLoadPointAction(Func<Task> action)
            {
                GoLoadPoint = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnBiasToughtAction(Func<Task> action)
            {
                OnBiasTought = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnRequestPermissionToAcceptAction(Func<Task> action)
            {
                RequestPermissionToAccept = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnRequestPermissionToStartAction(Func<Task> action)
            {
                RequestPermissionToStart = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnHasResultAction(Func<Task> action)
            {
                HasResult = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnSearchScorchAction(Func<Task> action)
            {
                SearchScorch = action;
                return this;
            }
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
