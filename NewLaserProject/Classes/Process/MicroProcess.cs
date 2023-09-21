using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using UnitsNet;

namespace NewLaserProject.Classes.Process
{
    internal class MicroProcess : BaseLaserProcess
    {
        private readonly EntityPreparator _entityPreparator;
        private readonly IMarkLaser _laserMachine;
        private readonly Func<double, Task> _funcForZBlock;
        private IProcObject _currentProcObject;

        public MicroProcess(string jsonPierce, EntityPreparator entityPreparator, IMarkLaser laserMachine, Func<double, Task> funcForZBlock) : base(jsonPierce)
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
            //await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true);
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
            var result = await _laserMachine.PierceDxfObjectAsync(fileHandler.FilePath).ConfigureAwait(false);
        }
        protected async override Task FuncForDelayBlockAsync(int delay)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);
        }
    }
}
