using System.Windows.Media;

namespace NewLaserProject.Classes.LogSinks.ConsoleSink
{
    public record MessageChunk(string Text, Brush Background, Brush Foreground, bool newline = false);
}
