using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface IProcess
    {
        void CreateProcess();
        Task StartAsync();
    }
}