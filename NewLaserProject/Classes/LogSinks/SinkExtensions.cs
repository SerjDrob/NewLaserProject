using System;
using System.Collections.Generic;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using NewLaserProject.Classes.LogSinks.RepositorySink;
using NewLaserProject.Classes.LogSinks.TokenRenderers;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace NewLaserProject.Classes.LogSinks
{
    public static class SinkExtensions
    {
        public static LoggerConfiguration RepoSink(
                  this LoggerSinkConfiguration loggerConfiguration,
                  WorkTimeLogger logger, params ILogEventFilter[] filters)
        {

            return loggerConfiguration.Sink(new RepoSink(logger, filters));
        }

        public static LoggerConfiguration WpfConsoleSink(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            WpfConsoleSink wpfConsoleSink,
            string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Properties} {Message:lj}{NewLine}{Exception}"
            )
        {
            var messageTemplate = new MessageTemplateParser().Parse(outputTemplate);
            var list = new List<OutputTemplateTokenRenderer>();
            foreach (MessageTemplateToken token in messageTemplate.Tokens)
            {
                if (token is TextToken textToken)
                {
                    list.Add(new TextTokenRenderer(textToken.Text));
                    continue;
                }

                var propertyToken = (PropertyToken)token;
                if (propertyToken.PropertyName == "Level")
                {
                    list.Add(new LevelTokenRenderer(propertyToken));
                }
                else if (propertyToken.PropertyName == "NewLine")
                {
                    list.Add(new NewLineTokenRenderer(propertyToken.Alignment));
                }
                else if (propertyToken.PropertyName == "TraceId")
                {
                    //list.Add(new TraceIdTokenRenderer(theme, propertyToken));
                }
                else if (propertyToken.PropertyName == "SpanId")
                {
                    //list.Add(new SpanIdTokenRenderer(theme, propertyToken));
                }
                else if (propertyToken.PropertyName == "Exception")
                {
                    list.Add(new ExceptionTokenRenderer(propertyToken));
                }
                else if (propertyToken.PropertyName == "Message")
                {
                    list.Add(new MessageTemplateOutputTokenRenderer(propertyToken));
                }
                else if (propertyToken.PropertyName == "Timestamp")
                {
                    list.Add(new TimestampTokenRenderer(propertyToken));
                }
                else if (propertyToken.PropertyName == "Properties")
                {
                    //list.Add(new PropertiesTokenRenderer(theme, propertyToken, messageTemplate, formatProvider));
                }
                else
                {
                    //list.Add(new EventPropertyTokenRenderer(theme, propertyToken, formatProvider));
                }
            }
            wpfConsoleSink.SetTokenRenderers(list);
            return loggerSinkConfiguration.Sink(wpfConsoleSink);
        }


        public static bool SourceContextEquals(this LogEvent logEvent, Type sourceContext)
            => logEvent.Properties.GetValueOrDefault("SourceContext") is ScalarValue sv && sv.Value?.ToString() == sourceContext.Assembly.FullName;

        public static bool SourceContextContains(this LogEvent logEvent, Type sourceContext)
        {
            if (logEvent.Properties.GetValueOrDefault("SourceContext") is ScalarValue sv)
            {
                var name1 = sv.Value?.ToString();
                var name2 = sourceContext.Assembly.GetName().Name;
                return name1.Contains(name2);
            }
            return false;
        }

    }
}
