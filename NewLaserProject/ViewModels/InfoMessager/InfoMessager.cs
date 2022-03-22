using System;
using System.IO;

namespace NewLaserProject.ViewModels
{
    public class InfoMessager
    {
        private string DANGERPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,"Views", "Sources", "danger.png");
        private string EXCLAMATIONPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Views", "Sources", "exclamation.png");
        private string INFOPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Views", "Sources", "info.png");
        private string PROCESSPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Views", "Sources", "process.png");
        private string LOADINGPATH = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Views", "Sources", "loading.png");
        public void RealeaseMessage(string message, Icon icon)
        {
            var iconPath = icon switch
            {
                Icon.Danger => DANGERPATH,
                Icon.Exclamation => EXCLAMATIONPATH,
                Icon.Info => INFOPATH,
                Icon.Process => PROCESSPATH,
                Icon.Loading => LOADINGPATH
            };
            PublishMessage?.Invoke(message, iconPath, icon);
        }
        public void EraseMessage()
        {
            PublishMessage?.Invoke(String.Empty, String.Empty, Icon.Empty);
        }
        public event Action<string, string, Icon> PublishMessage;

    }

}
