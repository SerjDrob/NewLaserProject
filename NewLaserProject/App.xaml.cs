using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Markers;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging;
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
            MachineConfiguration machineconfigs = ExtensionMethods
                .DeserilizeObject<MachineConfiguration>(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "MachineConfigs.json"))
                ?? throw new NullReferenceException("The machine configs are null");

            MainIoC = new ServiceCollection();

            MainIoC.AddMediatR(Assembly.GetExecutingAssembly())
                   //.AddSingleton<ISubject, Subject>()
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
                   .AddScoped<IVideoCapture, USBCamera>()
                   .AddSingleton<LaserMachine>()
                   .AddSingleton<MainViewModel>()
                   .AddDbContext<DbContext, LaserDbContext>()
                   .AddLogging(builder =>
                   {
                       builder.AddFile(ProjectPath.GetFilePathInFolder(ProjectFolders.TEMP_FILES, "app.log"));
                   })
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

            viewModel?.OnInitialized();
        }
    }


    internal static partial class LogExtensions
    {
        [LoggerMessage(EventId = 23,
            Level = LogLevel.Error,
            Message = "Motion failed with '{message}'")]
        public static partial void LogMotionException(this ILogger logger, string message);
        [LoggerMessage(12, LogLevel.Warning, "Fuck '{message}'")]
        public static partial void LogAppException(this ILogger logger, string message);
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

    class FileLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            throw new NotImplementedException();
        }
    }
}
