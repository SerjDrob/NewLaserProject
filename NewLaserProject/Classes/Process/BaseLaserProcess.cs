using System;
using System.Threading.Tasks;
using MachineClassLibrary.Laser.Parameters;
using NewLaserProject.Classes.ProgBlocks;

namespace NewLaserProject.Classes.Process 
{ 
    public abstract class BaseLaserProcess
{
    protected readonly ProgTreeParser _progTreeParser;
    protected readonly Func<Task> _pierceFunction;

    public BaseLaserProcess(string jsonPierce)
    {
        _progTreeParser = new(jsonPierce);

        _progTreeParser
            .SetModuleFunction<TaperBlock, double>(new FuncProxy<double>(FuncForTapperBlockAsync))
            .SetModuleFunction<AddZBlock, double>(new FuncProxy<double>(FuncForAddZBlockAsync))
            .SetModuleFunction<PierceBlock, ExtendedParams>(new FuncProxy<ExtendedParams>(FuncForPierseBlockAsync))
            .SetModuleFunction<DelayBlock, int>(new FuncProxy<int>(FuncForDelayBlockAsync));

        _pierceFunction = _progTreeParser
           .GetTree()
           .GetFunc();
    }

    protected abstract Task FuncForTapperBlockAsync(double tapper);
    protected abstract Task FuncForAddZBlockAsync(double z);
    protected abstract Task FuncForPierseBlockAsync(ExtendedParams extendedParams);
    protected abstract Task FuncForDelayBlockAsync(int delay);

}
}