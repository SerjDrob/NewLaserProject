using System;
using System.Diagnostics;
using System.Windows.Controls;
using HandyControl.Controls;

namespace NewLaserProject.Views
{
    /// <summary>
    /// Interaction logic for MainView2.xaml
    /// </summary>
    public partial class MainView : GlowWindow
    {
        public MainView()
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
            grid?.UnselectAll();
            if (grid?.SelectedItem is not null)
            {
                grid.UpdateLayout();
                grid.ScrollIntoView(grid.SelectedItem);
            }
        }

        private void AxisState_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        //private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        //{
        //    e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        //}
    }
}
