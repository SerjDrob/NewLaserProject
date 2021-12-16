using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace NewLaserProject.UserControls
{
    /// <summary>
    /// Interaction logic for DblTextBox.xaml
    /// </summary>
    public partial class DblTextBox : UserControl
    {
        public DblTextBox()
        {
            InitializeComponent();
            MyTextBox.DataContext=this;
        }


        public string DblText
        {
            get { return (string)GetValue(DblTextProperty); }
            set { SetValue(DblTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DblTextProperty =
            DependencyProperty.Register("DblText", typeof(string), typeof(DblTextBox), new PropertyMetadata(string.Empty));

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            
            Regex regex = new Regex(@"^[0-9]+");
            e.Handled = !regex.IsMatch(e.Text);
        }
    }
}
