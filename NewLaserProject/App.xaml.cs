using System;
using System.Configuration;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using AutoMapper;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Markers;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.Data;
using NewLaserProject.Properties;
using NewLaserProject.Repositories;
using NewLaserProject.ViewModels;
using NewLaserProject.ViewModels.DialogVM;
using NewLaserProject.ViewModels.DialogVM.Profiles;
using NewLaserProject.Views;

namespace NewLaserProject
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ServiceCollection MainIoC
        {
            get; private set;
        }
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();
        public App()
        {
            var machineconfigs = ExtensionMethods
                .DeserilizeObject<LaserMachineConfiguration>(AppPaths.MachineConfigs)
                ?? throw new NullReferenceException("The machine configs are null");

            var settingsManager = new SettingsManager<LaserMachineSettings>(AppPaths.LaserMachineSettings);
            settingsManager.Load();

            MainIoC = new ServiceCollection();

            _ = MainIoC.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()))
                   .AddSingleton<ISettingsManager<LaserMachineSettings>>(settingsManager)
                   .AddDbContext<DbContext, LaserDbContext>(options =>
                   {
                       var connectionString = ConfigurationManager.ConnectionStrings["myDb"].ToString();
                       options.UseSqlite(connectionString);
                   }, ServiceLifetime.Singleton)
                   .AddTransient(typeof(IRepository<>), typeof(LaserRepository<>))
                   .AddSingleton<ISubject<IProcessNotify>, Subject<IProcessNotify>>()
                   .AddScoped<MotDevMock>()
                   .AddScoped<MotionDevicePCI1240U>()
                   .AddScoped<MotionDevicePCI1245E>()
                   .AddSingleton(sp =>
                   {
                       var motionBoard = new MotionBoardFactory(sp, machineconfigs).GetMotionBoard();
                       motionBoard.SetPrecision(machineconfigs.AxesTolerance);
                       return motionBoard;
                   })
                   .AddSingleton<ExceptionsAgregator>()
                   .AddScoped<JCZLaser>()
                   .AddScoped<MockLaser>()
                   //.AddScoped<PWM3>()
                   .AddSingleton<PWM3>()
                   //.AddScoped<PWM2>()
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
                   .AddLogging(builder =>
                   {
                       builder.AddFile(AppPaths.Applog);
                       builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                       builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Error);
                   })
                   .AddAutoMapper(cfg =>
                   {
                       cfg.AddProfile<ParamsProfile>();
                       cfg.AddProfile<MarkParamsProfile>();
                   })
                   .AddTransient<LaserDbViewModel>(sp =>
                   {
                       var defLaserParams = ExtensionMethods
                            .DeserilizeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);

                       var mapper = sp.GetService<IMapper>();
                       var mediator = sp.GetService<IMediator>();
                       var defaultParams = mapper?.Map<ExtendedParams>(defLaserParams);
                       var loggerProvider = sp.GetRequiredService<ILoggerProvider>();
                       return new(mediator, defaultParams, loggerProvider);
                   })
                   .AddTransient<MarkSettingsVM>(sp =>
                   {
                       var defLaserParams = ExtensionMethods
                            .DeserilizeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);
                       var mapper = sp.GetService<IMapper>();
                       var vm = mapper?.Map<MarkSettingsVM>(defLaserParams);
                       return vm;
                   });
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _principleLogger.LogError(e.Exception, "An unhandled Exception was thrown");
        }
        private ILogger _principleLogger;
        protected override void OnStartup(StartupEventArgs e)
        {
            var provider = MainIoC.BuildServiceProvider();
            var loggerProvider = provider.GetRequiredService<ILoggerProvider>();
            _principleLogger = loggerProvider.CreateLogger("AppLogger");
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            var viewModel = provider.GetService<MainViewModel>();
            var context = provider.GetService<DbContext>() as LaserDbContext;
            context?.LoadSetsAsync();

            base.OnStartup(e);

            Trace.TraceInformation("The application started");
            Trace.Flush();

            new MainView()
            {
                DataContext = viewModel
            }.Show();

            viewModel?.OnInitialized();
            AllocConsole();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            var sourceString = ConfigurationManager.ConnectionStrings["myDb"].ToString();
            var destString = ConfigurationManager.ConnectionStrings["myDbBackup"].ToString();

            using (var location = new SqliteConnection(sourceString))
            using (var destination = new SqliteConnection(destString))
            {
                location.Open();
                destination.Open();
                location.BackupDatabase(destination);
            }
            base.OnDeactivated(e);
        }
    }
}
