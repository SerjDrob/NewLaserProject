using System;
using System.Windows.Media;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using Serilog.Events;
using Serilog.Parsing;

namespace NewLaserProject.Classes.LogSinks.TokenRenderers
{
    internal class LevelTokenRenderer : OutputTemplateTokenRenderer
    {
        private readonly PropertyToken _levelToken;

        public LevelTokenRenderer(PropertyToken levelToken)
        {
            _levelToken = levelToken;
        }
        public override void Render(LogEvent logEvent, out MessageChunk output) => output = logEvent.Level switch
        {
            LogEventLevel.Verbose => new MessageChunk("VRB", Brushes.Black, Brushes.Yellow),
            LogEventLevel.Debug => new MessageChunk ("DBG", Brushes.Black, Brushes.AliceBlue),
            LogEventLevel.Information => new MessageChunk ("INF", Brushes.Black, Brushes.White),
            LogEventLevel.Warning => new MessageChunk ("WRN", Brushes.DarkOrange, Brushes.Black),
            LogEventLevel.Error => new MessageChunk ("ERR", Brushes.Red, Brushes.White),
            LogEventLevel.Fatal => new MessageChunk("FTL", Brushes.Red, Brushes.Yellow),
            _ => throw new ArgumentException()
        };
    }
}
