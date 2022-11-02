using DicingBlade.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Markers;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MachineControlsLibrary.Classes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewLaserProject.Classes;
using NewLaserProject.Data;
using NewLaserProject.ViewModels;
using NewLaserProject.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NewLaserProject
{
    internal class ScopedGeomsRequest:IRequest<(IEnumerable<LayerGeometryCollection>,IEnumerable<Point>)> 
    {
        public ScopedGeomsRequest(double width, double height, double x, double y)
        {
            Width = width;
            Height = height;
            X = x;
            Y = y;
        }

        public double Width { get; init; }
        public double Height { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
    }

    public interface IReqHandler<TRequest, TResponse>
    {
        Task<TResponse> HandleAsync(TRequest request);
    }

    public class Mediator<TRequest, TResponse>
    {
        private Func<TRequest, Task<TResponse>> _handler;
        public void Subscribe(IReqHandler<TRequest,TResponse> reqHandler)
        {
            _handler = reqHandler.HandleAsync;
        }
        public async Task<TResponse> Send(TRequest request)
        {
            return await _handler?.Invoke(request);
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ServiceCollection MainIoC { get; private set; }
        public App()
        {
            var machineconfigs = ExtensionMethods
                .DeserilizeObject<MachineConfiguration>(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "MachineConfigs.json"));


            MainIoC = new ServiceCollection();

            MainIoC.AddMediatR(Assembly.GetExecutingAssembly())
                   .AddScoped<MotDevMock>()
                   .AddScoped<MotionDevicePCI1240U>()
                   .AddScoped<MotionDevicePCI1245E>()
                   .AddSingleton(sp =>
                   {
                        return new MotionBoardFactory(sp, machineconfigs).GetMotionBoard();
                   })
                   .AddSingleton<ExceptionsAgregator>()
                   .AddScoped<JCZLaser>()
                   .AddScoped<MockLaser>()
                   .AddScoped<PWM>()
                   .AddScoped<PWM2>()
                   .AddSingleton(sp =>
                   {
                       return new LaserBoardFactory(sp, machineconfigs).GetPWM();
                   })
                   .AddSingleton(sp =>
                   {
                       return new LaserBoardFactory(sp, machineconfigs).GetLaserBoard();
                   })
                   .AddScoped<IVideoCapture,USBCamera>()                   
                   .AddSingleton<LaserMachine>()
                   .AddSingleton<MainViewModel>()
                   .AddDbContext<DbContext, LaserDbContext>()
                   ;

            var listenerName = "myListener";
            Trace.Listeners.Add(new TextWriterTraceListener("MyLog.log", listenerName));
            
            //Trace.Listeners.Add(new MyTraceListener("MyLog.log", listenerName));

            Trace.Listeners[listenerName].TraceOutputOptions |= TraceOptions.DateTime;


            Trace.Write("Start", "Header");
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

    class MyTraceListener : TextWriterTraceListener
    {
        public MyTraceListener(string? fileName, string? name) : base(fileName, name)
        {
        }

        public override void Write(string? message, string? category)
        {
            base.Write(message, category);
        }

        public override void Flush()
        {
            base.Flush();
        }
    }
}
