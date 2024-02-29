using System.Windows.Media;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using Serilog.Events;
using Serilog.Parsing;

namespace NewLaserProject.Classes.LogSinks.TokenRenderers
{
    internal class TimestampTokenRenderer : OutputTemplateTokenRenderer
    {
        private readonly PropertyToken _propertyToken;

        public TimestampTokenRenderer(PropertyToken propertyToken)
        {
            _propertyToken = propertyToken;
        }
        public override void Render(LogEvent logEvent, out MessageChunk output)
        {
            output = new MessageChunk(logEvent.Timestamp.ToString(_propertyToken.Format), Brushes.Black, Brushes.White);
        }
    }
}
