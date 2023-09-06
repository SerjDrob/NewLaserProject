using System;
using System.IO;

namespace NewLaserProject.ViewModels
{
    public class InfoMessenger
    {
        private string DANGERPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,"Views", "Sources", "danger.png");
        private string EXCLAMATIONPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Views", "Sources", "exclamation.png");
        private string INFOPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Views", "Sources", "info.png");
        private string PROCESSPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Views", "Sources", "process.png");
        private string LOADINGPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Views", "Sources", "loading.png");
        public void RealeaseMessage(string message, MessageType icon)
        {
            var iconPath = icon switch
            {
                MessageType.Danger => DANGERPATH,
                MessageType.Exclamation => EXCLAMATIONPATH,
                MessageType.Info => INFOPATH,
                MessageType.Process => PROCESSPATH,
                MessageType.Loading => LOADINGPATH
            };
            PublishMessage?.Invoke(message, iconPath, icon);
        }
        public void EraseMessage()
        {
            PublishMessage?.Invoke(String.Empty, String.Empty, MessageType.Empty);
        }
        public event Action<string, string, MessageType> PublishMessage;

    }

}
