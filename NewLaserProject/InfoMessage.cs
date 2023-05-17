using NewLaserProject.Classes;
using NewLaserProject.ViewModels;

namespace NewLaserProject
{
    internal record InfoMessage(string Message, MessageType MessageType):IProcessNotify;
}
