using System.Collections.Generic;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using Serilog.Events;

namespace NewLaserProject.Classes.LogSinks.TokenRenderers
{
    internal abstract class OutputTemplateTokenRenderer
    {
        public abstract void Render(LogEvent logEvent, out IEnumerable<MessageChunk> output);
    }
}
