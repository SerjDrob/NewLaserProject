using HandyControl.Controls;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Miscellaneous;
using MachineClassLibrary.Settings;
using ScottPlot;
using Stateless;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NewLaserProject.Classes
{
    internal class ScanheadCalibrationTeacher : ITeacher
    {
        private readonly ICoorSystem<LMPlace> _coorSystem;
        private readonly LaserMachine _laserMachine;
        private readonly ISettingsManager<LaserMachineSettings> _settingsManager;
        private readonly double _waferThickness;
        private readonly double _width;
        private readonly double _height;
        private readonly double _diameter;
        private readonly double _arrayWidth;
        private readonly ArraySize _arraySize;
        private StateMachine<MyState, MyTrigger> _stateMachine;
        private IEnumerator<(double x, double y)> _enumerator;
        private bool _reentry = false;
        private IEnumerable<(double x, double y)> _resultArray;
        private List<(double x, double y)> _resultPoints;
        private List<(double x, double y)> _rawPoints;
        private const string _tempFileName = "tempPoints";

        public ScanheadCalibrationTeacher(ICoorSystem<LMPlace> coorSystem, LaserMachine laserMachine,
                ISettingsManager<LaserMachineSettings> settingsManager, double waferThickness, 
                double width, double height,
                double diameter, double arrayWidth, ArraySize arraySize)
        {
            _coorSystem = coorSystem;
            _laserMachine = laserMachine;
            _settingsManager = settingsManager;
            _waferThickness = waferThickness;
            _width = width;
            _height = height;
            _diameter = diameter;
            _arrayWidth = arrayWidth;
            _arraySize = arraySize;

            var zCamera = _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null");
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);
            (double x, double y) coordinate = (0, 0);

            if (_reentry) throw new Exception("The sequence has no elements.");
            var elementIndex = 0;
            var elementsCount = 0;


                _stateMachine.Configure(MyState.Begin)
                    .OnActivateAsync(async () =>
                    {
                        _enumerator = _resultArray
                            .Select(item =>
                            {
                                //var coor = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, item.x + _width / 2, item.y + _height / 2);

                                var coorX = (item.x + _width / 2 + _settingsManager.Settings.XLeftPoint) ?? throw new NullReferenceException();
                                var coorY = (item.y + _height / 2 + _settingsManager.Settings.YLeftPoint) ?? throw new NullReferenceException();

                                return (coorX, coorY);
                            })
                            .GetEnumerator();
                        elementsCount = _resultArray.Count();
                        if (File.Exists(@"D:\scan.txt")) File.Delete(@"D:\scan.txt");
                        if (File.Exists(@"D:\tempPoints.txt"))
                        {
                            var result = await File.ReadAllLinesAsync(@"D:\tempPoints.txt");
                            if (result.Length != 0)
                            {
                                if (MessageBox.Ask("Использовать точки из незавершённой калибровки?", "Калибровка") == System.Windows.MessageBoxResult.OK)
                                {
                                    foreach (var item in result)
                                    {
                                        var indPoint = JsonConvert.DeserializeObject<(double x, double y)>(item);
                                        _resultPoints ??= new();
                                        _resultPoints.Add(indPoint);
                                        _enumerator.MoveNext();
                                        elementIndex++;
                                        elementsCount--;
                                    }
                                }
                            }
                        }
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

                        _laserMachine.SetVelocity(Velocity.Service);

                        await Task.WhenAll(
                             _laserMachine.MoveAxInPosAsync(Ax.X, coordinate.x, true),
                             _laserMachine.MoveAxInPosAsync(Ax.Y, coordinate.y, true),
                             _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera - waferThickness)
                             );
                        _laserMachine.SetVelocity(Velocity.Step);
                        Growl.Info($"Точка {elementIndex++}, осталось: {elementsCount--}");
                        _reentry = _enumerator.MoveNext();
                    })
                    .OnExitAsync(async () =>
                    {
                        _resultPoints ??= new();
                        var x = _laserMachine.GetAxActual(Ax.X);
                        var y = _laserMachine.GetAxActual(Ax.Y);
                        _resultPoints.Add((x, y));

                        var str = (x, y);

                        var output = JsonConvert.SerializeObject(str);

                        await File.AppendAllLinesAsync(@"D:\tempPoints.txt", [output]);

                    })
                    .PermitReentryIf(MyTrigger.Next, () => _reentry)
                    .PermitIf(MyTrigger.Next, MyState.HasResult, () => !_reentry)
                    .Permit(MyTrigger.Deny, MyState.End)
                    .Ignore(MyTrigger.Accept);

                _stateMachine.Configure(MyState.HasResult)
                    .OnEntry(() =>
                    {
                        var centerNum = ((int)Math.Pow((int)_arraySize, 2) - 1) / 2;
                        var normPoint = _resultPoints.ElementAt(centerNum);
                        var filePoints = _resultPoints.Select(p => (p.x - normPoint.x, p.y - normPoint.y))
                            .ToList();
                        var first = _resultPoints.ElementAt(0);


                        var strings = new List<string>
                        {
                        "[VALUE]"
                        };
                        var i = 0;
                        foreach (var p in filePoints)
                        {
                            strings.Add($"{i} = {Math.Round(p.Item1, 4)}, {Math.Round(p.Item2, 4)}");
                            i++;
                        }

                        File.AppendAllLines(@"D:\scan.txt", strings);
                        //File.Delete(@"D:\tempPoints.txt");
                        Growl.Info($"Конец!");
                    })
                    .Permit(MyTrigger.Next,MyState.End);

                _stateMachine.Configure(MyState.End)
                    .OnEntry(() =>
                    {
                        TeachingCompleted?.Invoke(this, EventArgs.Empty);
                    });
        }

        public event EventHandler TeachingCompleted;

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

        public async Task StartTeachAsync()
        {
            Growl.Clear();
            _laserMachine.SetVelocity(Velocity.Service);
            var zCamera = _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null");
            var zLaser = _settingsManager.Settings.ZeroPiercePoint ?? throw new ArgumentNullException("ZeroPiercePoint is null");
            var defLaserParams = MiscExtensions
                      .DeserializeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);
            var pen = defLaserParams.PenParams; //with
            //{
            //    Freq = 20000,
            //    MarkSpeed = 200,
            //    MarkLoop = 1,
            //    QPulseWidth = 1
            //};

            var hatch = defLaserParams.HatchParams;
            _laserMachine.SetMarkParams(new(pen, hatch));
           // var center = _coorSystem.ToSub(LMPlace.FileOnWaferUnderLaser, _width / 2, _height / 2);

            var centerX = (_width / 2 + _settingsManager.Settings.XLeftPoint + _settingsManager.Settings.XOffset) ?? throw new NullReferenceException();
            var centerY = (_height / 2 + _settingsManager.Settings.YLeftPoint + _settingsManager.Settings.YOffset) ?? throw new NullReferenceException();



            await Task.WhenAll(
                _laserMachine.MoveAxInPosAsync(Ax.X, centerX, true),
                _laserMachine.MoveAxInPosAsync(Ax.Y, centerY, true),
                _laserMachine.MoveAxInPosAsync(Ax.Z, zLaser - _waferThickness)
                );
            _resultArray = await _laserMachine.MarkCircleArrayAsync(_diameter, _arrayWidth, (int)_arraySize);
            await _stateMachine.ActivateAsync();
        }
    }
}
