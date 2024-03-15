using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace NewLaserProject.Views.Controls
{
    /// <summary>
    /// Interaction logic for SpeedIndicator.xaml
    /// </summary>
    public partial class SpeedIndicator : UserControl
    {
        public SpeedIndicator()
        {
            InitializeComponent();
        }


        public bool SwitchArrow
        {
            get { return (bool)GetValue(SwitchArrowProperty); }
            set { SetValue(SwitchArrowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SwitchArrow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SwitchArrowProperty =
            DependencyProperty.Register("SwitchArrow", typeof(bool), typeof(SpeedIndicator), new PropertyMetadata(false));



        public int Speed
        {
            get { return (int)GetValue(SpeedProperty); }
            set { SetValue(SpeedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FastSpeed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(int), typeof(SpeedIndicator), new PropertyMetadata(0, ArrowChanged));



        private static void ArrowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpeedIndicator indicator)
            {
                var storyName = (int)e.NewValue switch
                {
                    0 => "GoToFast",
                    1 => "GoToSlow",
                    2 => "GoToStep"
                };
                var story = indicator.MyGrid.Resources[storyName] as Storyboard;
                if (story != null) story.Begin();
                //else
                //{
                //    var anim = new DoubleAnimation(0d, TimeSpan.FromSeconds(1));
                //    indicator.SpeedArrow.BeginAnimation(Canvas.LeftProperty, anim);
                //}
            }
        }
    }
}
