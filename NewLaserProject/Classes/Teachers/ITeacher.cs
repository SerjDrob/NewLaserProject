using System;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface ITeacher
    {
        Task AcceptAsync();
        Task DenyAsync();
        Task NextAsync();
        Task StartTeachAsync();
        event EventHandler TeachingCompleted;
        void SetParams(params double[] ps);
        double[] GetParams();
        void SetResult(double result);
    }
}
