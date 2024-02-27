using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NewLaserProject.Classes.LogSinks;

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
            ConsoleOutput.ItemsSource = _controls;
            //var text = new TextBlock { Text = "Hello World", FontFamily = new FontFamily("Consolas") };
            //_controls.Add(text);
        }

        ObservableCollection<FrameworkElement> _controls;
        public void SetMessage(FrameworkElement control) => _controls.Add(control);

        public WpfConsoleSink ConsoleSink
        {
            get { return (WpfConsoleSink)GetValue(ConsoleSinkProperty); }
            set { SetValue(ConsoleSinkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConsoleSink.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConsoleSinkProperty =
            DependencyProperty.Register("ConsoleSink", typeof(WpfConsoleSink), typeof(WpfConsole.WPFConsole), new PropertyMetadata(null, ConsoleSinkChanged));

        private static void ConsoleSinkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var console = d as WPFConsole;
            if (console != null) 
            {
                console.ConsoleSink.OfType<ConsoleMessage>()
                    .Subscribe(msg =>
                    {
                        var panel = new StackPanel { Orientation = Orientation.Horizontal };
                        foreach (var chunk in msg.MsgChunks)
                        {
                            var text = new TextBlock { 
                                Text = chunk.Text, 
                                FontFamily = new FontFamily("Consolas"),
                                Background = chunk.Background,
                                Foreground = chunk.Foreground
                            };
                            panel.Children.Add(text);
                        }
                        console.SetMessage(panel);           
                    });
            }
        }
    }
}
