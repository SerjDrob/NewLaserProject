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
            //var machine = new MotionDevicePCI1240U();
            MainIoC = new ServiceCollection();
            MainIoC//.AddSingleton(typeof(MotionDevicePCI1240U),typeof(MotionDevicePCI1245E))
                   .AddSingleton<MotionDevicePCI1240U>()
                   .AddSingleton<ExceptionsAgregator>()
                   .AddScoped(typeof(IMarkLaser), typeof(JCZLaser))
                   .AddScoped(typeof(IVideoCapture), typeof(USBCamera))
                   .AddSingleton<LaserMachine>()
                   .AddSingleton<MainViewModel>();

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            var provider = MainIoC.BuildServiceProvider();

#if PCIInserted
            var viewModel = provider.GetService<MainViewModel>();
#else
            var viewModel = new MainViewModel();
#endif   
            base.OnStartup(e);
            new MainView()
            {
                DataContext = viewModel
            }.Show();
        }
    }
}
