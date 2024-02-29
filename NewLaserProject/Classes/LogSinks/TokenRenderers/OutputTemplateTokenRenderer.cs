using NewLaserProject.Classes.LogSinks.ConsoleSink;
using Serilog.Events;

namespace NewLaserProject.Classes.LogSinks.TokenRenderers
{
    internal abstract class OutputTemplateTokenRenderer
    {
        public abstract void Render(LogEvent logEvent, out MessageChunk output);
    }
}
