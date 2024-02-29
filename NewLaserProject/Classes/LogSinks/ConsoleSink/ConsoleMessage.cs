using System.Collections.Generic;

namespace NewLaserProject.Classes.LogSinks.ConsoleSink
{
    public record ConsoleMessage(IEnumerable<MessageChunk> MsgChunks);
}
