using DicingBlade.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Markers;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using NewLaserProject.ViewModels;
using NewLaserProject.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NewLaserProject
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {        
        protected override void OnStartup(StartupEventArgs e)
        {

            //var exceptionAgregator = new ExceptionsAgregator();
            //var motionDevice = new MotionDevicePCI1245E();
            //var markLaser = new JCZLaser();
            //var camera = new USBCamera();
            //var machine = new LaserMachine(exceptionAgregator, motionDevice, markLaser, camera);
            base.OnStartup(e);
            new MainView()
            {
                DataContext = new MainViewModel()
            }.Show();
        }
    }
}
