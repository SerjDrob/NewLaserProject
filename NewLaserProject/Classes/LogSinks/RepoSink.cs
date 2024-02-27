using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using System.Windows.Media;
using MachineClassLibrary.Miscellaneous;
using Newtonsoft.Json;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Parsing;
using Serilog.Sinks.SystemConsole.Themes;
using UnitsNet;

namespace NewLaserProject.Classes.LogSinks
{
    public class RepoSink : ILogEventSink
    {
        private readonly WorkTimeLogger _workTimeLogger;
        private readonly ILogEventFilter[] _filters;

        public static string Start => "{Start}";
        public static string End => "{End}";
        public static string Cancelled => "{Cancelled}";
        public static string Failed => "{Failed}";
        public static string App => "App";
        public static string Proc => "Proc";
        public static string ProcArgs => "{ProcArgs}";

        private string GetName(string str) => str.Trim('{', '}');

        public RepoSink(WorkTimeLogger workTimeLogger, params ILogEventFilter[] filters)
        {
            _workTimeLogger = workTimeLogger;
            _filters = filters;
        }
        public async void Emit(LogEvent logEvent)
        {
            if (Matching.WithProperty<string>(GetName(Start), p => p.Equals(App)).Invoke(logEvent))
            {
                await _workTimeLogger.LogAppStarted();
                return;
            }
            if (Matching.WithProperty<string>(GetName(End), p => p.Equals(App)).Invoke(logEvent))
            {
                await _workTimeLogger.LogAppStopped();
                return;
            }
            if (Matching.WithProperty<string>(GetName(Failed), p => p.Equals(App)).Invoke(logEvent))
            {
                await _workTimeLogger.LogAppFailed(logEvent.Exception);
                return;
            }
            if (Matching.WithProperty<string>(GetName(ProcArgs), p =>
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<ProcStartedArgs>(p);
                    if (result is not null) _workTimeLogger.LogProcessStarted(result.fileName, result.materialName, result.technologyName, result.materialThickness);
                }
                catch (Exception)
                {
                }
                return true;
            }).Invoke(logEvent)) return;
            if (Matching.WithProperty<string>(GetName(End), p => p.Equals(Proc)).Invoke(logEvent))
            {
                await _workTimeLogger.LogProcessEnded();
                return;
            }
            if (Matching.WithProperty<string>(GetName(Cancelled), p => p.Equals(Proc)).Invoke(logEvent))
            {
                await _workTimeLogger.LogProcessCancelled();
                return;
            }
            if (Matching.WithProperty<string>(GetName(Failed), p => p.Equals(Proc)).Invoke(logEvent))
            {
                await _workTimeLogger.LogProcessFailed(logEvent.Exception);
                return;
            }
        }
    }


    public class WpfConsoleSink : ILogEventSink, IObservable<ConsoleMessage>
    {
        readonly ISubject<ConsoleMessage> _subject;
        private List<IDisposable>? _subscriptions;
        private List<OutputTemplateTokenRenderer> _list;

        public WpfConsoleSink()
        {
            _subject = new Subject<ConsoleMessage>();
        }
        internal void SetTokenRenderers(IEnumerable<OutputTemplateTokenRenderer> outputTemplateTokens) => _list = outputTemplateTokens.ToList();
        public void Emit(LogEvent logEvent)
        {
            var chunks = new List<MessageChunk>();
            foreach (var render in _list)
            {
                render.Render(logEvent, out var messageChunk);
                chunks.Add(messageChunk);
            }
            _subject.OnNext(new ConsoleMessage(chunks));
        }
        public IDisposable Subscribe(IObserver<ConsoleMessage> observer)
        {
            _subscriptions ??= new List<IDisposable>();
            var subscription = _subject.Subscribe(observer);
            _subscriptions.Add(subscription);
            return subscription;
        }
    }

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
            if(!logEvent.SourceContextContains(typeof(App))) font = Brushes.CornflowerBlue;
            output = new MessageChunk(writer.ToString(), Brushes.Black, font);
        }
    }

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
    internal abstract class OutputTemplateTokenRenderer
    {
        public abstract void Render(LogEvent logEvent, out MessageChunk output);
    }



    public record MessageChunk(string Text, Brush Background, Brush Foreground);
    public record ConsoleMessage(IEnumerable<MessageChunk> MsgChunks);
    public class ProcStartedArgs
    {
        public string fileName { get; set; }
        public string materialName { get; set; }
        public string technologyName { get; set; }
        public double materialThickness { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class OnlyForContextFilter<TContext> : ILogEventFilter
    {
        public bool IsEnabled(LogEvent logEvent)
        {
            return Matching.FromSource<TContext>().Invoke(logEvent);
        }
    }

    public static class Filters
    {
        public static OnlyForContextFilter<TContext> OnlyForContextFilter<TContext>() => new OnlyForContextFilter<TContext>();
    }

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
                    //list.Add(new NewLineTokenRenderer(propertyToken.Alignment));
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
                    //list.Add(new ExceptionTokenRenderer(theme, propertyToken));
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
            => logEvent.Properties.GetValueOrDefault("SourceContext") is ScalarValue sv && sv.Value?.ToString() == sourceContext.FullName;

        public static bool SourceContextContains(this LogEvent logEvent, Type sourceContext)
            => logEvent.Properties.GetValueOrDefault("SourceContext") is ScalarValue sv && (sv.Value?.ToString()?.Contains(sourceContext.FullName) ?? false);

    }
}
