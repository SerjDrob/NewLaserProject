using System;
using System.Threading;
using System.Threading.Tasks;
using MachineClassLibrary.Laser.Parameters;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures;

namespace NewLaserProject.Classes.Process
{
    public abstract class BaseLaserProcess
    {
        //protected readonly ProgTreeParser _progTreeParser;
        protected readonly Func<Task> _pierceFunction;
        protected readonly ProcessingSequence _procSequence;
        protected readonly CancellationTokenSource _cancellationTokenSource;

        public BaseLaserProcess(string jsonPierce)
        {
            _cancellationTokenSource = new();
            /*
            _progTreeParser = new(jsonPierce, _cancellationTokenSource.Token);

            _progTreeParser
                .SetModuleFunction<TaperBlock, double>(new FuncProxy<double>(FuncForTapperBlockAsync))
                .SetModuleFunction<AddZBlock, double>(new FuncProxy<double>(FuncForAddZBlockAsync))
                .SetModuleFunction<PierceBlock, ExtendedParams>(new FuncProxy<ExtendedParams>(FuncForPierseBlockAsync))
                .SetModuleFunction<DelayBlock, int>(new FuncProxy<int>(FuncForDelayBlockAsync));

            _pierceFunction = _progTreeParser
               .GetTree()
               .GetFunc();
            */

            _procSequence = ProgTreeParser2.GetProgBlocksSequence(jsonPierce);

        }

        protected async Task ProcessSequenceAsync()
        {
            foreach (var block in _procSequence)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested) continue;
                await (block switch
                {
                    TaperBlock t => FuncForTapperBlockAsync(t.Tapper),
                    AddZBlock z => FuncForAddZBlockAsync(z.DeltaZ),
                    PierceBlock p => FuncForPierseBlockAsync(p.MarkParams),
                    DelayBlock d => FuncForDelayBlockAsync(d.DelayTime),
                    _ => Task.CompletedTask
                });
            }
        }

        public CancellationTokenSource GetCancellationTokenSource() => _cancellationTokenSource;
        protected abstract Task FuncForTapperBlockAsync(double tapper);
        protected abstract Task FuncForAddZBlockAsync(double z);
        protected abstract Task FuncForPierseBlockAsync(ExtendedParams extendedParams);
        protected abstract Task FuncForDelayBlockAsync(int delay);

    }
}
