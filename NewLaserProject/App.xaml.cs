using DicingBlade.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Markers;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewLaserProject.Classes;
using NewLaserProject.Data;
using NewLaserProject.ViewModels;
using NewLaserProject.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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


            var conf = new LaserAxesConfiguration
            {
                XLine = 6.4,
                YLine = 6.4,
                XRightDirection = true,
                YRightDirection = true,
                ZRightDirection = true,
                ZToObjectiveAproximation = true
            };

            conf.SerializeObject(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "AxesConfigs.json"));

            var machineconfigs = ExtensionMethods
                .DeserilizeObject<MachineConfiguration>(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "MachineConfigs.json"));


            MainIoC = new ServiceCollection();

            MainIoC.AddScoped<MotDevMock>()
                   .AddScoped<MotionDevicePCI1240U>()
                   .AddScoped<MotionDevicePCI1245E>()
                   .AddSingleton(sp =>
                    {
                        return new MotionBoardFactory(sp, machineconfigs).GetMotionBoard();
                    })
                   .AddSingleton<ExceptionsAgregator>()
                   .AddScoped<JCZLaser>()
                   .AddScoped<MockLaser>()
                   .AddSingleton(sp =>
                    {
                       return new LaserBoardFactory(sp, machineconfigs).GetLaserBoard();
                    })
                   .AddScoped<IVideoCapture,USBCamera>()                   
                   .AddSingleton<LaserMachine>()
                   .AddSingleton<MainViewModel>()
                   .AddDbContext<DbContext, LaserDbContext>()
                   .AddMediatR(Assembly.GetExecutingAssembly());

            var listenerName = "myListener";
            Trace.Listeners.Add(new TextWriterTraceListener("MyLog.log", listenerName));
            Trace.Listeners[listenerName].TraceOutputOptions |= TraceOptions.DateTime;
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            var provider = MainIoC.BuildServiceProvider();

#if PCIInserted

            var viewModel = provider.GetService<MainViewModel>();
#else
            var db = provider.GetService<LaserDbContext>();
            var mediator = provider.GetService<IMediator>();
            var viewModel = new MainViewModel(db,mediator);
#endif   
            base.OnStartup(e);
            
            Trace.TraceInformation("The application started");
            Trace.Flush();

            new MainView2()
            {
                DataContext = viewModel
            }.Show();
        }
    }
}
