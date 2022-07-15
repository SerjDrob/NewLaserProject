using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface IProcess
    {
        void CreateProcess();
        Task Deny();
        Task Next();
        Task StartAsync();
        Task StartAsync(CancellationToken cancellationToken);
    }
}