using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.GeometryUtility;
using NewLaserProject.Classes.ProgBlocks;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
//using System.IO;

namespace NewLaserProject.Classes
{

    public class LaserProcess : IProcess
    {
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private bool _inLoop = false;
        private int _loopCount = 0; 
        private PierceParams _pierceParams;
        private ProgTreeParser _progTreeParser;
        private List<IProcObject> _excludedObjects;
        private readonly double _zPiercing;
        private readonly double _waferThickness;
        private readonly EntityPreparator _entityPreparator;
        private readonly ISubject<IProcessNotify> _subject;
        private List<IDisposable> _subscriptions;
        private readonly bool _underCamera;

        public event EventHandler<IEnumerable<IProcObject>> CurrentWaferChanged;
        public event EventHandler<(IProcObject,int)> ProcessingObjectChanged;
        public event EventHandler<ProcessCompletedEventArgs> ProcessingCompleted;

        public LaserProcess(IEnumerable<IProcObject> wafer, string jsonPierce, LaserMachine laserMachine,
            ICoorSystem coorSystem, double zPiercing, double waferThickness, EntityPreparator entityPreparator)
        {
            _underCamera = false;// true;
            _wafer = wafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
            _zPiercing = zPiercing;
            _waferThickness = waferThickness;
            _entityPreparator = entityPreparator;
            _subject=new Subject<IProcessNotify>();


            //try
            //{
            //    _wafer.Select(obj => _coorSystem.ToGlobal(obj.X, obj.Y))
            //           .Select(coor => new { x = coor[0], y = coor[1] })
            //           .ToList()
            //           .SerializeObject(ProjectPath.GetFilePathInFolder(ProjectFolders.DATA, $"tempWaf - {Guid.NewGuid()}"));
            //}
            //catch (Exception ex)
            //{

            //    throw;
            //}
        }


        public void CreateProcess()
        {   
            _progTreeParser = new ProgTreeParser(_jsonPierce);

            var currentIndex = -1;
            var waferEnumerator = _progTreeParser.MainLoopShuffle ? _wafer.Shuffle().GetEnumerator()
                            : _wafer.GetEnumerator();
            var coorFile = new StreamWriter(ProjectPath.GetFilePathInFolder(ProjectPath.GetFolderPath("TempFiles"), "coorFile"));
            _progTreeParser
                //TODO don't pass the taper, it should be calculated value respective the taper and other tech params like specified tolerance
                .SetModuleFunction<TaperBlock, double>(new FuncProxy<double>(taper => _entityPreparator.SetEntityContourOffset(taper)))
                .SetModuleFunction<AddZBlock, double>(new FuncProxy<double>(async z => { await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true); }))
                .SetModuleFunction<PierceBlock, ExtendedParams>(new FuncProxy<ExtendedParams>(async mlp => { await Pierce(mlp, waferEnumerator.Current); }))
                .SetModuleFunction<DelayBlock, int>(new FuncProxy<int>(Task.Delay));

            var pierceFunction = _progTreeParser
                .GetTree()
                .GetFunc();

            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);

            _stateMachine.Configure(State.Started)
                .OnActivate(() => { _inLoop = waferEnumerator.MoveNext(); currentIndex++; })
                .Ignore(Trigger.Pause)
                .Permit(Trigger.Deny, State.Denied)
                .Permit(Trigger.Next, State.Working);

            _stateMachine.Configure(State.Working)
                .OnEntryAsync(async () => 
                {                    
                    var procObject = waferEnumerator.Current;
                    ProcessingObjectChanged?.Invoke(this, (procObject, currentIndex));
                    
                    _subject.OnNext(new ProcObjectChanged(procObject));

                    if (!_excludedObjects?.Any(o=>o.Id==procObject.Id) ?? true)
                    {
                        var position = _coorSystem.ToGlobal(procObject.X, procObject.Y);
                        _laserMachine.SetVelocity(Velocity.Fast);
                        if(!_underCamera) await Task.WhenAll(
                        //_laserMachine.MoveGpInPosAsync(Groups.XY, position, true),
                        
                        _laserMachine.MoveAxInPosAsync(Ax.Y, position[1], true),
                        _laserMachine.MoveAxInPosAsync(Ax.X, position[0],true),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing - _waferThickness));
                        await Task.Delay(300);
                        await Task.WhenAll(
                            //_laserMachine.MoveGpInPosAsync(Groups.XY, position, true),

                            _laserMachine.MoveAxInPosAsync(Ax.Y, position[1], true),
                            _laserMachine.MoveAxInPosAsync(Ax.X, position[0], true)//,
                           /* _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing - _waferThickness)*/);
                        procObject.IsBeingProcessed = true;

                        if (_inProcess && !_underCamera)
                        {
                            await pierceFunction();
                        }
                        else
                        {
                            await Task.Delay(1000);
                            var pointsLine = String.Empty;
                            var line = $" X: { Math.Round(procObject.X, 3),8 } | Y: { Math.Round(procObject.Y, 3),8} | X: { Math.Round(position[0], 3),8} | Y: { Math.Round(position[1], 3),8} | X: { Math.Round(_laserMachine.GetAxActual(Ax.X), 3),8} | Y: { Math.Round(_laserMachine.GetAxActual(Ax.Y), 3),8}";
                            if (HandyControl.Controls.MessageBox.Ask("Good?") == System.Windows.MessageBoxResult.OK)
                            {
                                pointsLine = "GOOD" + line;
                            }
                            else
                            {
                                pointsLine = $"BAD " + line;
                            }
                            await coorFile.WriteLineAsync(pointsLine);
                            await coorFile.FlushAsync();
                        }
                        procObject.IsProcessed = true;
                    }
                    ProcessingObjectChanged?.Invoke(this, (procObject, currentIndex));

                    _subject.OnNext(new ProcObjectChanged(procObject));

                    _inLoop = waferEnumerator.MoveNext();
                    currentIndex++;
                })
                .PermitReentryIf(Trigger.Next, () => _inLoop)
                .PermitIf(Trigger.Next, State.Loop,() => !_inLoop)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Loop)
                .OnEntry(() =>
                {
                    _loopCount++;
                    var currentWafer = _progTreeParser.MainLoopShuffle ? _wafer.Shuffle() : _wafer;
                    CurrentWaferChanged?.Invoke(this,currentWafer);

                    _subject.OnNext(new ProcWaferChanged(currentWafer));

                    waferEnumerator = currentWafer.GetEnumerator();
                    currentIndex = -1;
                    _inLoop = waferEnumerator.MoveNext();
                    currentIndex++;
                })
                .OnExit(() => _inProcess = false)
                .PermitIf(Trigger.Next, State.Working, () => _loopCount < _progTreeParser.MainLoopCount)
                .PermitIf(Trigger.Next, State.Exit, () => _loopCount >= _progTreeParser.MainLoopCount);

            _stateMachine.Configure(State.Exit)
                .OnEntry(() => 
                {
                    ProcessingCompleted?.Invoke(this, new ProcessCompletedEventArgs(CompletionStatus.Success, _coorSystem));

                    _subject.OnNext(new ProcCompletionPreview(CompletionStatus.Success, _coorSystem));
                    //_subject.OnCompleted();
                })
                .Ignore(Trigger.Next);

            _stateMachine.Activate();

            async Task Pierce(ExtendedParams markLaserParams, IProcObject procObject)
            {
                using (var fileHandler = _entityPreparator.GetPreparedEntityDxfHandler(procObject))
                {
                    _laserMachine.SetExtMarkParams(new ExtParamsAdapter(markLaserParams));
                    var result = await _laserMachine.PierceDxfObjectAsync(fileHandler.FilePath);
                }
            }
        }

        public async Task StartAsync()
        {
            if (_stateMachine is null) CreateProcess();
            _inProcess = true;

            for (int i = 0; i < _progTreeParser.MainLoopCount; i++)
            {
                while (_inProcess)
                {
                    await _stateMachine.FireAsync(Trigger.Next);
                }
            }
            Trace.TraceInformation("The process ended");
            Trace.Flush();
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (_stateMachine is null) CreateProcess();
            _inProcess = true;

            for (int i = 0; i < _progTreeParser.MainLoopCount; i++)
            {
                while (_inProcess)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _inProcess = false;
                        continue;
                    }
                    await _stateMachine.FireAsync(Trigger.Next);
                }
            }
            if (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("The process ended");
                Trace.Flush();
            }
            else
            {
                Trace.TraceInformation("The process was interupted by user");
                Trace.Flush();
                ProcessingCompleted?.Invoke(this, new ProcessCompletedEventArgs(CompletionStatus.Cancelled,_coorSystem));
                _subject.OnNext(new ProcCompletionPreview(CompletionStatus.Cancelled, _coorSystem));
            }
        }
        public override string ToString()
        {
            return UmlDotGraph.Format(_stateMachine.GetInfo());
        }

        public async Task Deny()
        {
            if (_inProcess)
            {
                _inProcess = false;
                Trace.TraceInformation("The process was cancelled by user");
                Trace.Flush();
                var success = await _laserMachine.CancelMarkingAsync();                
            }
        }

        public Task Next()
        {
            throw new NotImplementedException();
        }

        public void ExcludeObject(IProcObject procObject)
        {
            _excludedObjects ??= new();
            _excludedObjects.Add(procObject);
        }

        public void IncludeObject(IProcObject procObject)
        {
            try
            {
                var obj = _excludedObjects.Single(o => o.Id == procObject.Id);
                if (obj is not null)
                {
                    _excludedObjects.Remove(obj);
                }
            }
            catch (Exception)
            {

                //throw;
            }
        }

        public IDisposable Subscribe(IObserver<IProcessNotify> observer)
        {
            _subscriptions ??= new();
            var subscription = _subject.Subscribe(observer);
            _subscriptions.Add(subscription);
            return subscription;
        } 

        public void Dispose()
        {
            _subscriptions?.ForEach(s=>s.Dispose());
        }

        enum State
        {
            Started,
            Working,
            Paused,
            Denied,
            Loop,
            Exit
        }
        enum Trigger
        {
            Next,
            Pause,
            Deny
        }
    }
        
    //public abstract class BaseLaserProcess
    //{
    //    private readonly ProgTreeParser _progTreeParser;
    //    protected Func<Task> pierceFunction;

    //    public BaseLaserProcess(ProgTreeParser progTreeParser)
    //    {
    //        _progTreeParser = progTreeParser;

    //        _progTreeParser
    //            .SetModuleFunction<TapperBlock>(FuncForTapperBlock)
    //            .SetModuleFunction<AddZBlock>(FuncForAddZBlock)
    //            .SetModuleFunction<PierceBlock>(FuncForPierseBlock)
    //            .SetModuleFunction<DelayBlock>(FuncForDelayBlock);

    //        pierceFunction = _progTreeParser
    //           .GetTree()
    //           .GetFunc();
    //    }

    //    protected abstract Task FuncForTapperBlock(double tapper);
    //    protected abstract Task FuncForAddZBlock(double z);
    //    protected abstract Task FuncForPierseBlock(ExtendedParams extendedParams);
    //    protected abstract Task FuncForDelayBlock(int delay);

    //}
}
