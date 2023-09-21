namespace NewLaserProject.Classes.Process.ProcessFeatures
{
    public record ProcessMessage(string Message, MsgType MessageType) : IProcessNotify;
}
