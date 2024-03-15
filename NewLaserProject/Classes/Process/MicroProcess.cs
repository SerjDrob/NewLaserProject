using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using NewLaserProject.Classes.Process.ProcessFeatures;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using UnitsNet;

namespace NewLaserProject.Classes.Process
{
    internal class MicroProcess : BaseLaserProcess, IObservable<IProcessNotify>, IDisposable
    {
        private readonly EntityPreparator _entityPreparator;
        private readonly IMarkLaser _laserMachine;
        private readonly Func<double, Task> _funcForZBlock;
        private ISubject<IProcessNotify> _subject;
        private IProcObject _currentProcObject;
        private bool disposedValue;
        private List<IDisposable> _subscriptions;

        public MicroProcess(string jsonPierce, EntityPreparator entityPreparator, 
            IMarkLaser laserMachine, Func<double, Task> funcForZBlock) : base(jsonPierce)
        {
            _entityPreparator = entityPreparator;
            _laserMachine = laserMachine;
            _funcForZBlock = funcForZBlock;
        }
        public void SetEntityAngle(double angle) => _entityPreparator.SetEntityAngle(angle);
        public void AddEntityAngle(double angle) => _entityPreparator.AddEntityAngle(angle);
        public async Task InvokePierceFunctionForObjectAsync(IProcObject procObject)
        {
            _currentProcObject = procObject;
            await _pierceFunction.Invoke();
        }
        public int GetMainLoopCount() => _progTreeParser.MainLoopCount;
        public bool IsLoopShuffle => _progTreeParser.MainLoopShuffle;
        protected override Task FuncForTapperBlockAsync(double tapper)
        {
            _entityPreparator.SetEntityContourOffset(tapper);
            return Task.CompletedTask;
        }
        protected async override Task FuncForAddZBlockAsync(double z)
        {
            _subject.OnNext(new ChangingZ(z));
            await _funcForZBlock(z).ConfigureAwait(false);
        }
        protected async override Task FuncForPierseBlockAsync(ExtendedParams extendedParams)
        {
            if (extendedParams.EnableMilling) _entityPreparator.SetEntityContourWidth(0d);

            var offsetum = Length.FromMicrometers(extendedParams.ContourOffset);
            var widthum = Length.FromMicrometers(extendedParams.HatchWidth);

            _entityPreparator.SetEntityContourOffset(offsetum.Millimeters);
            _entityPreparator.SetEntityContourWidth(extendedParams.EnableMilling ? widthum.Millimeters : 0d);


            using var fileHandler = _entityPreparator.GetPreparedEntityDxfHandler(_currentProcObject);
            _laserMachine.SetExtMarkParams(new ExtParamsAdapter(extendedParams));
            _subject.OnNext(new PiercingWithParams(extendedParams));
            var result = await _laserMachine.PierceDxfObjectAsync(fileHandler.FilePath).ConfigureAwait(false);
        }
        protected async override Task FuncForDelayBlockAsync(int delay)
        {
            _subject.OnNext(new Delaying(delay));
            await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _subscriptions.ForEach(s=>s.Dispose());
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MicroProcess()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public IDisposable Subscribe(IObserver<IProcessNotify> observer)
        {
            _subject ??= new Subject<IProcessNotify>();
            var subscription = _subject.Subscribe(observer);
            _subscriptions ??= new();
            _subscriptions.Add(subscription);
            return subscription;
        }
    }
    public record PiercingWithParams(ExtendedParams ExtParams):IProcessNotify;
    public record ChangingZ(double Z):IProcessNotify;
    public record Delaying(int Delay):IProcessNotify;
    public record MainLoopChanged(int Loop):IProcessNotify;
}
