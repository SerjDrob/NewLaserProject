namespace NewLaserProject.Classes
{
    public record ProcessMessage(string Message, MsgType MessageType) : IProcessNotify;



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
