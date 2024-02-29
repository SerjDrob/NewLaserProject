using System;
using Newtonsoft.Json;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

namespace NewLaserProject.Classes.LogSinks.RepositorySink
{
    public class RepoSink : ILogEventSink
    {
        private readonly WorkTimeLogger _workTimeLogger;
        private readonly ILogEventFilter[] _filters;

        public static string Start => "{Start}";
        public static string End => "{End}";
        public static string Cancelled => "{Cancelled}";
        public static string Failed => "{Failed}";
        public static string App => "App";
        public static string Proc => "Proc";
        public static string ProcArgs => "{ProcArgs}";

        private string GetName(string str) => str.Trim('{', '}');

        public RepoSink(WorkTimeLogger workTimeLogger, params ILogEventFilter[] filters)
        {
            _workTimeLogger = workTimeLogger;
            _filters = filters;
        }
        public async void Emit(LogEvent logEvent)
        {
            if (Matching.WithProperty<string>(GetName(Start), p => p.Equals(App)).Invoke(logEvent))
            {
                await _workTimeLogger.LogAppStarted();
                return;
            }
            if (Matching.WithProperty<string>(GetName(End), p => p.Equals(App)).Invoke(logEvent))
            {
                await _workTimeLogger.LogAppStopped();
                return;
            }
            if (Matching.WithProperty<string>(GetName(Failed), p => p.Equals(App)).Invoke(logEvent))
            {
                await _workTimeLogger.LogAppFailed(logEvent.Exception);
                return;
            }
            if (Matching.WithProperty<string>(GetName(ProcArgs), p =>
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<ProcStartedArgs>(p);
                    if (result is not null) _workTimeLogger.LogProcessStarted(result.FileName, result.MaterialName, result.TechnologyName, result.MaterialThickness);
                }
                catch (Exception)
                {
                }
                return true;
            }).Invoke(logEvent)) return;
            if (Matching.WithProperty<string>(GetName(End), p => p.Equals(Proc)).Invoke(logEvent))
            {
                await _workTimeLogger.LogProcessEnded();
                return;
            }
            if (Matching.WithProperty<string>(GetName(Cancelled), p => p.Equals(Proc)).Invoke(logEvent))
            {
                await _workTimeLogger.LogProcessCancelled();
                return;
            }
            if (Matching.WithProperty<string>(GetName(Failed), p => p.Equals(Proc)).Invoke(logEvent))
            {
                await _workTimeLogger.LogProcessFailed(logEvent.Exception);
                return;
            }
        }
    }
}
