using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface ITeacher
    {
        Task Accept();
        Task Deny();
        Task Next();
        Task StartTeach();
        void SetParams(params double[] ps);
        double[] GetParams();
    }
}