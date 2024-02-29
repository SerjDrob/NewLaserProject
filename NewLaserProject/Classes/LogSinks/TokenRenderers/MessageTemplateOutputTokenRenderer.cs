using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public override void Render(LogEvent logEvent, out IEnumerable<MessageChunk> output)
        {
            if (logEvent.Properties.ContainsKey("SourceContext") && !logEvent.SourceContextContains(typeof(App)))
            {
                var writer = new StringWriter();
                logEvent.RenderMessage(writer);
                var font = Brushes.CornflowerBlue;
                output = Enumerable.Repeat(new MessageChunk(writer.ToString(), Brushes.Black, font), 1);
            }
            else
            {
                output = logEvent.CRenderMessage();
            }
        }
    }


    internal static class LogEventExtensions
    {
        public static IEnumerable<MessageChunk> CRenderMessage(this  LogEvent logEvent)
        {
            var list = new List<MessageChunk>();
            foreach (var token in logEvent.MessageTemplate.Tokens)
            {
                if(token is TextToken text)
                {
                    list.Add(new MessageChunk(text.Text, Brushes.Black, Brushes.White));
                }
                else if(token is PropertyToken property)
                {
                    if (logEvent.Properties.TryGetValue(property.PropertyName, out var value))
                    {
                        var str = value.ToString();
                        list.Add(new MessageChunk(str, Brushes.Black, str.Equals("null") ? Brushes.DodgerBlue : Brushes.LawnGreen));
                    }
                    else
                    {
                        list.Add(new MessageChunk("", Brushes.Black, Brushes.White));
                    }
                }
            }
            return list;
        }
    }
}
