﻿using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MediatR;
using Microsoft.Toolkit.Diagnostics;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Classes.Mediator;
using NewLaserProject.ViewModels;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes.Process
{
    internal class ThreePointProcesSnap : IProcess
    {
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private readonly double _zeroZPiercing;
        private readonly double _zeroZCamera;
        private readonly double _waferThickness;
        private CancellationTokenSource _ctSource;
        private readonly InfoMessager _infoMessager;
        private double _xActual;
        private double _yActual;
        private readonly double _dX;
        private readonly double _dY;
        private readonly double _pazAngle;
        private readonly ISubject<INotification> _mediator;
        private readonly EntityPreparator _entityPreparator;
        private double _matrixAngle;
        private IProcess _subProcess;
        
        public ThreePointProcesSnap(IEnumerable<IProcObject> wafer,
            string jsonPierce, LaserMachine laserMachine, ICoorSystem<LMPlace> coorSystem,
            double zeroZPiercing, double zeroZCamera, double waferThickness, InfoMessager infoMessager,
            double dX, double dY, double pazAngle, EntityPreparator entityPreparator, ISubject<INotification> mediator)
        {
            _wafer = wafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
            _zeroZPiercing = zeroZPiercing;
            _zeroZCamera = zeroZCamera;
            _infoMessager = infoMessager;
            _dX = dX;
            _dY = dY;
            _pazAngle = pazAngle;
            _entityPreparator = entityPreparator;
            _waferThickness = waferThickness;
            _ctSource = new CancellationTokenSource();
            _mediator = mediator;
        }


        public void CreateProcess()
        {
            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);
            var resultPoints = new List<PointF>();
            var originPoints = new List<PointF>();
            
            _mediator.OfType<SnapShotResult>()
                .Subscribe(async result =>
                {
                    originPoints.Add(result);
                    try
                    {
                        await _stateMachine?.FireAsync(Trigger.Next);
                    }
                    catch (Exception)
                    {

                       // throw;
                    }
                });
            
            _mediator.OfType<ReadyForSnap>()
                .Subscribe(result => 
                {
                    var position = _coorSystem.FromGlobal(_xActual, _yActual);
                    var request = new ScopedGeomsRequest(5000, 5000, position[0]*1000, position[1]*1000);
                    _mediator.OnNext(request);
                });
            
            CoorSystem<LMPlace> workCoorSys = new();
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;

            SwitchCamera?.Invoke(this, true);

            _stateMachine.Configure(State.Started)
                .OnActivate(() => _infoMessager.RealeaseMessage("Next", ViewModels.MessageType.Info))
                .Permit(Trigger.Next, State.GoRefPoint)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GoRefPoint)
                .OnEntry(() =>
                {
                    _mediator.OnNext(new PermitSnap(true));
                    _infoMessager.RealeaseMessage($"Сопоставьте точку {originPoints.Count + 1}", ViewModels.MessageType.Info);
                })
                .OnExit(() =>
                {
                    resultPoints.Add(new((float)(_xActual + _dX), (float)(_yActual + _dY)));
                })
                .PermitReentryIf(Trigger.Next, () => resultPoints.Count < 2)
                .PermitIf(Trigger.Next, State.GetRefPoint, () => resultPoints.Count == 2)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GetRefPoint)
                .OnEntry(_infoMessager.EraseMessage)
                .OnEntry(() =>
                {
                    _mediator.OnNext(new PermitSnap(false));
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    SwitchCamera?.Invoke(this, false);
                    workCoorSys = new CoorSystem<LMPlace>
                    .ThreePointCoorSystemBuilder<LMPlace>()
                    .SetFirstPointPair(originPoints[0], resultPoints[0])
                    .SetSecondPointPair(originPoints[1], resultPoints[1])
                    .SetThirdPointPair(originPoints[2], resultPoints[2])
                    .FormWorkMatrix(0.001, 0.001, false)
                    .Build();
                    _matrixAngle = workCoorSys.GetMatrixAngle();
                    _stateMachine.Fire(Trigger.Next);
                })
                .Permit(Trigger.Next, State.Working)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Working)
                .OnEntryAsync(async () => {
                    _entityPreparator.SetEntityAngle(-_pazAngle - _matrixAngle + Math.PI);//TODO make add entity angle method/ fix it for Laserprocess. Get angle from outside!!!
                    _subProcess = new LaserProcess(_wafer, _jsonPierce, _laserMachine, workCoorSys,
                    _zeroZPiercing, _waferThickness, _entityPreparator);
                    _subProcess.CurrentWaferChanged += _process_CurrentWaferChanged;
                    _subProcess.ProcessingObjectChanged += _process_ProcessingObjectChanged;
                    _subProcess.ProcessingCompleted += _subProcess_ProcessingCompleted;
                    _subProcess.CreateProcess();
                    await _subProcess.StartAsync(_ctSource.Token);
                })
                .Ignore(Trigger.Next)
                .Ignore(Trigger.Deny)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Denied)
                .OnEntryAsync(async () =>
                {
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    ProcessingCompleted?.Invoke(this, new ProcessCompletedEventArgs(CompletionStatus.Cancelled, _coorSystem));
                    //ctSource.Cancel();
                    //_infoMessager.RealeaseMessage("Процесс отменён", ViewModels.Icon.Exclamation);
                });
        }

        private void _subProcess_ProcessingCompleted(object? sender, ProcessCompletedEventArgs args)
        {
            ProcessingCompleted?.Invoke(this, args);
        }

        private void _process_ProcessingObjectChanged(object? sender, (IProcObject, int) e)
        {
            ProcessingObjectChanged?.Invoke(sender, e);
        }

        private void _process_CurrentWaferChanged(object? sender, IEnumerable<IProcObject> e)
        {
            CurrentWaferChanged?.Invoke(sender, e);
        }

        public event EventHandler<bool> SwitchCamera;
        public event EventHandler<IEnumerable<IProcObject>> CurrentWaferChanged;
        public event EventHandler<(IProcObject, int)> ProcessingObjectChanged;
        public event EventHandler<ProcessCompletedEventArgs> ProcessingCompleted;

        private void _laserMachine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            switch (e.Axis)
            {
                case Ax.X:
                    _xActual = e.Position;
                    break;
                case Ax.Y:
                    _yActual = e.Position;
                    break;
                default:
                    break;
            }
        }

        public override string ToString()
        {
            return UmlDotGraph.Format(_stateMachine.GetInfo());
        }
        public async Task StartAsync()
        {
            CreateProcess();
            await _stateMachine.ActivateAsync();
        }

        public async Task Next()
        {
            try
            {
                await _stateMachine?.FireAsync(Trigger.Next);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task Deny() //=> await _stateMachine.FireAsync(Trigger.Deny);
        {
            _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
            _ctSource.Cancel();
            var success = await _laserMachine.CancelMarkingAsync();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void ExcludeObject(IProcObject procObject)
        {
            _subProcess?.ExcludeObject(procObject);
        }

        public void IncludeObject(IProcObject procObject)
        {
            _subProcess?.IncludeObject(procObject);
        }

        enum State
        {
            Started,
            GoRefPoint,
            GetRefPoint,
            Working,
            Paused,
            Denied
        }
        enum Trigger
        {
            Next,
            Pause,
            Deny
        }
    }
}
