using System;
using System.Threading.Tasks;
using MediatR;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.ProcTimeFeatures.Create;
using NewLaserProject.Data.Models.WorkTimeFeatures.Create;
using NewLaserProject.Data.Models.WorkTimeFeatures.Update;

namespace NewLaserProject.Classes
{
    public class WorkTimeLogger
    {
        private readonly IMediator _mediator;

        public WorkTimeLogger(IMediator mediator) => _mediator = mediator;

        private WorkTimeLog _currentAppWorkTimeLog;
        private ProcTimeLog? _currentProcWorkTimeLog;

        public async Task LogAppStarted()
        {
            var log = new WorkTimeLog
            {
                StartTime = DateTime.Now
            };
            var current = await _mediator.Send(new CreateWorkTimeLogRequest(log));
            _currentAppWorkTimeLog = current.WorkTimeLog;
        }
        public async Task LogAppStopped()
        {
            _currentAppWorkTimeLog.EndTime = DateTime.Now;
            await _mediator.Send(new UpdateWorkTimeRequest(_currentAppWorkTimeLog));
        }
        public async Task LogAppFailed(Exception exception)
        {
            _currentAppWorkTimeLog.EndTime = DateTime.Now;
            _currentAppWorkTimeLog.ExceptionMessage = exception.ToString();
            await _mediator.Send(new UpdateWorkTimeRequest(_currentAppWorkTimeLog));
        }
        public void LogProcessStarted(string fileName, string materialName, string technologyName, double materialThickness)
        {
            var yieldTime = DateTime.Now - (_currentProcWorkTimeLog?.EndTime ?? _currentAppWorkTimeLog.StartTime);
            _currentProcWorkTimeLog = new()
            {
                StartTime = DateTime.Now,
                FileName = fileName,
                MaterialName = materialName,
                TechnologyName = technologyName,
                MaterialThickness = materialThickness,
                YieldTime = yieldTime,
                WorkTimeLog = _currentAppWorkTimeLog
            };
        }
        public async Task LogProcessEnded() => await LogProcessEnded(null, true);
        public async Task LogProcessCancelled() => await LogProcessEnded(null, false);
        public async Task LogProcessFailed(Exception exception) => await LogProcessEnded(exception, false);
        private async Task LogProcessEnded(Exception? exception, bool success)
        {
            if (_currentProcWorkTimeLog is not null && _currentProcWorkTimeLog.EndTime == default)
            {
                _currentProcWorkTimeLog.EndTime = DateTime.Now;
                _currentProcWorkTimeLog.Success = success;
                _currentProcWorkTimeLog.ExceptionMessage = exception?.ToString();
                try
                {
                    await _mediator.Send(new CreateProcTimeRequest(_currentProcWorkTimeLog));
                }
                catch (Exception ex)
                {

                    throw;
                }
            }
        }

    }
}
