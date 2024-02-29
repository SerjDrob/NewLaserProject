using System.IO;
using System.Windows.Media;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using Serilog.Events;
using Serilog.Parsing;

namespace NewLaserProject.Classes.LogSinks.TokenRenderers
{
    internal class MessageTemplateOutputTokenRenderer : OutputTemplateTokenRenderer
    {
        private readonly PropertyToken propertyToken;

        public MessageTemplateOutputTokenRenderer(PropertyToken propertyToken)
        {
            this.propertyToken = propertyToken;
        }
        public override void Render(LogEvent logEvent, out MessageChunk output)
        {
            var writer = new StringWriter();
            logEvent.RenderMessage(writer);
            var font = Brushes.White;
            if (logEvent.Properties.ContainsKey("SourceContext") && !logEvent.SourceContextContains(typeof(App))) font = Brushes.CornflowerBlue;
            output = new MessageChunk(writer.ToString(), Brushes.Black, font);
        }
    }
}
