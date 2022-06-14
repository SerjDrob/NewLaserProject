using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NewLaserProject.Views.DbViews
{
    /// <summary>
    /// Interaction logic for AddToDbContainerView.xaml
    /// </summary>
    public partial class AddToDbContainerView : PopupWindow
    {
        public AddToDbContainerView()
        {
            InitializeComponent();
        }
        public AddToDbContainerView(bool showButtons)
        {
            InitializeComponent();
            Buttons.Visibility = showButtons ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
