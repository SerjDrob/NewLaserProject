using System;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface ITeacher
    {
        Task Accept();
        Task Deny();
        Task Next();
        Task StartTeach();
        event EventHandler TeachingCompleted;
        void SetParams(params double[] ps);
        double[] GetParams();
    }
}