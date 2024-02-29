using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using NewLaserProject.Classes.LogSinks.TokenRenderers;
using Serilog.Core;
using Serilog.Events;

namespace NewLaserProject.Classes.LogSinks.ConsoleSink
{
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
                chunks.AddRange(messageChunk);
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
}
