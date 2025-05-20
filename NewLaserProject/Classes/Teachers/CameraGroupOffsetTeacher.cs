using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HandyControl.Controls;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Miscellaneous;
using MachineClassLibrary.Settings;
using Stateless;

namespace NewLaserProject.Classes;

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
    private List<OffsetPoint> _offsetPointsCmd = new();
    private List<(double actX, double actY, double X, double Y)> _actualPoints;
    private List<(double cmdX, double cmdY, double X, double Y)> _commandPoints;
    private IEnumerator<(double actX, double actY, double cmdX, double cmdY, double X, double Y)> _enumerator;
    private bool _reentry;

    public event EventHandler? TeachingCompleted;

    public CameraGroupOffsetTeacher(ICoorSystem coorSystem, LaserMachine laserMachine,
        ISettingsManager<LaserMachineSettings> settingsManager, double waferThickness, double width, double height, IEnumerable<(double, double)> points)
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




        _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);
        //(double x, double y) coordinate = (0,0);
        (double xact, double yact, double xcmd, double ycmd, double x, double y) coordinate = (0, 0, 0, 0, 0, 0);

        if (_reentry) throw new Exception("The sequence has no elements.");


        _stateMachine.Configure(MyState.Begin)
            .OnActivate(() =>
            {
                var result = _actualPoints.Zip(_commandPoints,
                    (act, cmd) => (act.actX, act.actY, cmd.cmdX, cmd.cmdY, act.X, act.Y));
                _enumerator = result.GetEnumerator();// _coordinates.GetEnumerator(); // TODO it is disposable
                _reentry = !_enumerator.MoveNext();
                Growl.Info(""" Для начала обучения нажмите "*" """);
            })
            .Permit(MyTrigger.Next, MyState.UnderCamera)
            .Ignore(MyTrigger.Accept)
            .Ignore(MyTrigger.Deny);


        _stateMachine.Configure(MyState.UnderCamera)
            .OnEntryAsync(async () =>
            {
                Growl.Clear();
                coordinate = _enumerator.Current;


                await Task.WhenAll(
                     _laserMachine.MoveAxInPosAsync(Ax.X, coordinate.x, true),
                     _laserMachine.MoveAxInPosAsync(Ax.Y, coordinate.y, true),
                     _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera - waferThickness)
                     );
                Growl.Info(""" Совместите перекрестие и нажмите "*" """);
                _reentry = _enumerator.MoveNext();
            })
            .OnExit(() =>
            {
                //_settingsManager.Settings.OffsetPoints.GetOffsetByCurCoor(coordinate.x, coordinate.y, ref _xOffset, ref _yOffset);

                //_xOffset = coordinate.x + _xOffset - _laserMachine.GetAxActual(Ax.X);
                //_yOffset = coordinate.y + _yOffset - _laserMachine.GetAxActual(Ax.Y); 
                _xOffset = coordinate.xact - _laserMachine.GetAxActual(Ax.X);
                _yOffset = coordinate.yact - _laserMachine.GetAxActual(Ax.Y);
                var xOffsetCmd = coordinate.xcmd - _laserMachine.GetAxCmd(Ax.X);
                var yOffsetCmd = coordinate.ycmd - _laserMachine.GetAxCmd(Ax.Y);
                _offsetPoints.Add(new(
                    _laserMachine.GetAxActual(Ax.X),
                    _laserMachine.GetAxActual(Ax.Y),
                    _xOffset,
                    _yOffset
                    ));

                _offsetPointsCmd.Add(new(
                    _laserMachine.GetAxCmd(Ax.X),
                    _laserMachine.GetAxCmd(Ax.Y),
                    xOffsetCmd,
                    yOffsetCmd
                    ));



                Growl.Info("Смещение добавлено");
            })
            .PermitReentryIf(MyTrigger.Next, () => _reentry)
            .PermitIf(MyTrigger.Next, MyState.HasResult, () => !_reentry)
            .Permit(MyTrigger.Deny, MyState.End)
            .Ignore(MyTrigger.Accept);

        _stateMachine.Configure(MyState.HasResult)
            .OnEntry(() =>
            {
                if (_settingsManager.Settings.OffsetPoints != null)
                {
                    _settingsManager.Settings.OffsetPoints.AddRange(_offsetPoints);
                    if (_settingsManager.Settings.CmdOffsetPoints != null)
                    {
                        _settingsManager.Settings.CmdOffsetPoints.AddRange(_offsetPointsCmd);
                    }
                    else
                    {
                        _settingsManager.Settings.CmdOffsetPoints = _offsetPointsCmd;
                    }
                }
                else
                {
                    _settingsManager.Settings.OffsetPoints = _offsetPoints;
                    _settingsManager.Settings.CmdOffsetPoints = _offsetPointsCmd;
                }
                TeachingCompleted?.Invoke(this, EventArgs.Empty);
            })
            .OnEntry(() =>
            {
                _settingsManager.Save();
            });

        _stateMachine.Configure(MyState.End)
            .OnEntry(() =>
            {
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
            Freq = 30000,
            MarkSpeed = 200,
            ModDutyCycle = 10,
            MarkLoop = 1,
            QPulseWidth = 2
        };
        _actualPoints = new List<(double actX, double actY, double X, double Y)>();
        _commandPoints = new List<(double cmdX, double cmdY, double X, double Y)>();

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


            var actX = _laserMachine.GetAxActual(Ax.X);
            var actY = _laserMachine.GetAxActual(Ax.Y);
            var cmdX = _laserMachine.GetAxCmd(Ax.X);
            var cmdY = _laserMachine.GetAxCmd(Ax.Y);

            _actualPoints.Add((actX, actY, coordinate.x, coordinate.y));
            _commandPoints.Add((cmdX, cmdY, coordinate.x, coordinate.y));

            for (int i = 0; i < 2; i++)
            {
                await _laserMachine.PierceLineAsync(-0.5, 0, 0.5, 0);
                await _laserMachine.PierceLineAsync(0, -0.5, 0, 0.5);

                await _laserMachine.PierceCircleAsync(0.3);
            }

            await _laserMachine.PierceLineAsync(-0.4, 0.4, 0.4, 0.4);
            await _laserMachine.PierceLineAsync(-0.4, -0.4, 0.4, -0.4);
            await _laserMachine.PierceLineAsync(-0.4, -0.4, -0.4, 0.4);
            await _laserMachine.PierceLineAsync(0.4, -0.4, 0.4, 0.4);
        }

        await _stateMachine.ActivateAsync();
    }

}
enum MyState
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
enum MyTrigger
{
    Next,
    Accept,
    Deny
}

public enum ArraySize
{
    Array_5x5 = 5,
    Array_9x9 = 9,
    Array_13x13 = 13,
    Array_17x17 = 17,
    Array_33x33 = 33,
}
