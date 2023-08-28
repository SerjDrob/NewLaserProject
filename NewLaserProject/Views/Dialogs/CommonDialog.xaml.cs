using System;
using System.Windows.Controls;
using System.Windows.Input;
using HandyControl.Interactivity;

namespace NewLaserProject.Views.Dialogs;
/// <summary>
/// Interaction logic for CommonDialog.xaml
/// </summary>
public partial class CommonDialog : UserControl
{
    public void SetTitle(string title) => Title.Text = title;
    public CommonDialog()
    {
        InitializeComponent();
    }
}
