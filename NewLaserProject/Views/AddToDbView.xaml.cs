﻿using HandyControl.Controls;
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

namespace NewLaserProject.Views
{
    /// <summary>
    /// Interaction logic for AddMaterialToDbView.xaml
    /// </summary>
    public partial class AddToDbView : PopupWindow
    {
        public AddToDbView()
        {
            InitializeComponent();           
        }

        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}