using System.Windows.Media;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using Serilog.Events;
using Serilog.Parsing;

namespace NewLaserProject.Classes.LogSinks.TokenRenderers
{
    internal class ExceptionTokenRenderer : OutputTemplateTokenRenderer
    {
        private readonly PropertyToken _propertyToken;

        public ExceptionTokenRenderer(PropertyToken propertyToken)
        {
            _propertyToken = propertyToken;
        }
        public override void Render(LogEvent logEvent, out MessageChunk output)
        {
            var ex = logEvent.Exception;
            output = new MessageChunk(ex?.ToString() ?? "", Brushes.Black, Brushes.Salmon);
        }
    }
}
