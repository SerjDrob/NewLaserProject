using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

namespace NewLaserProject.Classes.LogSinks.Filters
{
    public class OnlyForContextFilter<TContext> : ILogEventFilter
    {
        public bool IsEnabled(LogEvent logEvent)
        {
            return Matching.FromSource<TContext>().Invoke(logEvent);
        }
    }
}
