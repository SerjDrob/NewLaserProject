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
using System.Diagnostics;

namespace NewLaserProject.Views
{
    /// <summary>
    /// Interaction logic for MainView2.xaml
    /// </summary>
    public partial class MainView2 : GlowWindow
    {
        public MainView2()
        {
            InitializeComponent();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Trace.TraceInformation("The application closed");
            Trace.Flush();
            Environment.Exit(0);
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = e.Source as DataGrid;

            if (grid?.SelectedItem is not null)
            {
                grid.UpdateLayout();
                grid.ScrollIntoView(grid.SelectedItem, null);
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
