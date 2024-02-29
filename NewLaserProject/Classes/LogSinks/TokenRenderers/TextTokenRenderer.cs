using System.Windows.Media;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using Serilog.Events;

namespace NewLaserProject.Classes.LogSinks.TokenRenderers
{
    internal class TextTokenRenderer : OutputTemplateTokenRenderer
    {
        private readonly string _text;

        public TextTokenRenderer(string text)
        {
            _text = text;
        }
        public override void Render(LogEvent logEvent, out MessageChunk output)
        {
            output = new MessageChunk(_text, Brushes.Black, Brushes.White);
        }
    }
}
