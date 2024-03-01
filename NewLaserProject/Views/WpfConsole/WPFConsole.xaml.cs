using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using NewLaserProject.Classes.LogSinks.ConsoleSink;

namespace NewLaserProject.Views.WpfConsole
{
    /// <summary>
    /// Interaction logic for WPFConsole.xaml
    /// </summary>
    public partial class WPFConsole : UserControl
    {
        public WPFConsole()
        {
            InitializeComponent();
            _controls = new();
            _controls.CollectionChanged += _controls_CollectionChanged;
            ConsoleOutput.ItemsSource = _controls;
        }

        private void _controls_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => Scroller.ScrollToEnd();

        ObservableCollection<FrameworkElement> _controls;
        public void SetMessage(FrameworkElement control) => _controls.Add(control);

        public WpfConsoleSink ConsoleSink
        {
            get { return (WpfConsoleSink)GetValue(ConsoleSinkProperty); }
            set { SetValue(ConsoleSinkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConsoleSink.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConsoleSinkProperty =
            DependencyProperty.Register("ConsoleSink", typeof(WpfConsoleSink), typeof(WPFConsole), new PropertyMetadata(null, ConsoleSinkChanged));

        private static void ConsoleSinkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var console = d as WPFConsole;

            if (console != null)
            {
                console.ConsoleSink.OfType<ConsoleMessage>()
                    .Subscribe(msg =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var block = new TextBlock { TextWrapping = TextWrapping.WrapWithOverflow };
                            var num = 0;
                            var count = msg.MsgChunks.Count();
                            foreach (var chunk in msg.MsgChunks)
                            {
                                num++;
                                var run = new Run
                                {
                                    Text = chunk.Text,
                                    FontFamily = new FontFamily("Consolas"),
                                    Background = chunk.Background,
                                    Foreground = chunk.Foreground,
                                };
                                if (chunk.newline && num != count) block.Inlines.Add(new LineBreak());
                                block.Inlines.Add(run);
                            }
                            console.SetMessage(block);
                        });
                    });
            }
        }
    }
}
