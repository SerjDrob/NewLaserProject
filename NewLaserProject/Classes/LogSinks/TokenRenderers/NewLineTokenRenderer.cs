using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using Serilog.Events;
using Serilog.Parsing;

namespace NewLaserProject.Classes.LogSinks.TokenRenderers
{
    internal class NewLineTokenRenderer : OutputTemplateTokenRenderer
    {
        private readonly Alignment? _alignment;

        public NewLineTokenRenderer(Alignment? alignment)
        {
            _alignment = alignment;
        }
        public override void Render(LogEvent logEvent, out IEnumerable<MessageChunk> output)
        {
            /*
            if (_alignment.HasValue)
            {
                if(_alignment.Value.)
            }
            */
            output = Enumerable.Repeat(new MessageChunk("", Brushes.Black, Brushes.White, true),1);
        }
    }
}
