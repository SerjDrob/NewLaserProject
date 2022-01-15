using DicingBlade.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Markers;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using Microsoft.Extensions.DependencyInjection;
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
        public ServiceCollection MainIoC { get; private set; }
        public App()
        {
            MainIoC=new ServiceCollection();
            MainIoC.AddSingleton(typeof(MotionDevicePCI1240U),typeof(MotionDevicePCI1245E))
                   .AddSingleton<ExceptionsAgregator>()
                   .AddScoped(typeof(IMarkLaser),typeof(JCZLaser))
                   .AddScoped(typeof(IVideoCapture),typeof(USBCamera))
                   .AddSingleton<LaserMachine>()
                   .AddSingleton<MainViewModel>();            

        }
        protected override void OnStartup(StartupEventArgs e)
        {

            //var exceptionAgregator = new ExceptionsAgregator();
            //var motionDevice = new MotionDevicePCI1245E();
            //var markLaser = new JCZLaser();
            //var camera = new USBCamera();
            //var machine = new LaserMachine(exceptionAgregator, motionDevice, markLaser, camera);
            var provider = MainIoC.BuildServiceProvider();
            var viewModel = provider.GetService<MainViewModel>();
            base.OnStartup(e);
            new MainView()
            {
                DataContext = viewModel//new MainViewModel()
            }.Show();
        }
    }
}
