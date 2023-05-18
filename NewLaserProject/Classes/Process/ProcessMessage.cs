namespace NewLaserProject.Classes
{
    public record ProcessMessage(string Message, MsgType MessageType) : IProcessNotify;
}
