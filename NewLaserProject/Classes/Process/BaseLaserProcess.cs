using System;
using System.Threading;
using System.Threading.Tasks;
using MachineClassLibrary.Laser.Parameters;
using NewLaserProject.Classes.Process.Utility;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures;

namespace NewLaserProject.Classes.Process
{
    public abstract class BaseLaserProcess
    {
        protected readonly ProgTreeParser _progTreeParser;
        protected readonly Func<Task> _pierceFunction;
        protected readonly CancellationTokenSource _cancellationTokenSource;

        public BaseLaserProcess(string jsonPierce)
        {
            _cancellationTokenSource = new();
            _progTreeParser = new(jsonPierce, _cancellationTokenSource.Token);

            _progTreeParser
                .SetModuleFunction<TaperBlock, double>(new FuncProxy<double>(FuncForTapperBlockAsync))
                .SetModuleFunction<AddZBlock, double>(new FuncProxy<double>(FuncForAddZBlockAsync))
                .SetModuleFunction<PierceBlock, ExtendedParams>(new FuncProxy<ExtendedParams>(FuncForPierseBlockAsync))
                .SetModuleFunction<DelayBlock, int>(new FuncProxy<int>(FuncForDelayBlockAsync));

            _pierceFunction = _progTreeParser
               .GetTree()
               .GetFunc();
        }
        public CancellationTokenSource GetCancellationTokenSource() => _cancellationTokenSource;
        protected abstract Task FuncForTapperBlockAsync(double tapper);
        protected abstract Task FuncForAddZBlockAsync(double z);
        protected abstract Task FuncForPierseBlockAsync(ExtendedParams extendedParams);
        protected abstract Task FuncForDelayBlockAsync(int delay);

    }
}