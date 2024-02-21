using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using MachineClassLibrary.Miscellaneous;
using MachineClassLibrary.VideoCapture;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.Logging;
using NewLaserProject.Classes;
using NewLaserProject.Classes.LogSinks;
using NewLaserProject.Classes.Process;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.Data;
using NewLaserProject.Data.Models;
using NewLaserProject.Properties;
using NewLaserProject.Repositories;
using NewLaserProject.ViewModels;
using NewLaserProject.ViewModels.DialogVM;
using NewLaserProject.ViewModels.DialogVM.Profiles;
using NewLaserProject.Views;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using SQLitePCL;

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



            var result = settingsManager.Settings.GetType().GetProperties().Any(p => p.GetValue(settingsManager.Settings)!= default);
            if (!result)
            {
                var configs = new MapperConfiguration(conf =>
                {
                    conf.CreateMap<Settings,LaserMachineSettings>()
                    .ForMember(ls=>ls.PreferredCameraCapabilities, opt=>opt.MapFrom(s=>s.PreferedCameraCapabilities));
                });
                var mapper = new Mapper(configs);
                var settings = mapper.Map<LaserMachineSettings>(Settings.Default);
                settingsManager.SetSettings(settings);
                settingsManager.Save();
            }

            MainIoC = new ServiceCollection();

            _ = MainIoC.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()))
                   .AddSingleton<ISettingsManager<LaserMachineSettings>>(settingsManager)
                   .AddDbContext<DbContext, LaserDbContext>(options =>
                   {
                       var connectionString = ConfigurationManager.ConnectionStrings["myDb"].ToString();
                       options.UseSqlite(connectionString);
                   }, ServiceLifetime.Singleton)
                   .AddDbContext<WorkTimeDbContext>(options =>
                   {
                       var connectionString = ConfigurationManager.ConnectionStrings["worktimeDB"].ToString();
                       options.UseSqlite(connectionString);
                   })
                   .AddTransient(typeof(IRepository<>), typeof(LaserRepository<>))
                   .AddSingleton<WorkTimeLogger>()
                   .AddTransient<IRepository<WorkTimeLog>, WorkTimeLogRepository<WorkTimeLog>>()
                   .AddTransient<IRepository<ProcTimeLog>, WorkTimeLogRepository<ProcTimeLog>>()
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
                   .AddScoped<JCZLaser>()
                   .AddScoped<MockLaser>()
                   .AddScoped<WorkTimeStatisticsVM>()
                   .AddSingleton<PWM3>()
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
                   .AddSerilog((sp, lc) =>
                   {
                       var wtlogger = sp.GetRequiredService<WorkTimeLogger>();
                       lc.WriteTo.Console(/*Serilog.Events.LogEventLevel.Information, "[{Timestamp:HH:mm:ss} {Level:u3}] {Properties} {Message:lj}{NewLine}{Exception}"*/)
                         .WriteTo.Logger(lc => lc
                         .Filter.ByExcluding(le => le.SourceContextEquals(typeof(MicroProcess)))
                         .WriteTo.File(AppPaths.SerilogFile, Serilog.Events.LogEventLevel.Information))
                         .WriteTo.RepoSink(wtlogger, Filters.OnlyForContextFilter<MicroProcess>())
                         .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        //.CreateLogger()
                        ;
                   })
                   //.AddLogging(builder =>
                   //{
                   //    builder.AddFile(AppPaths.Applog);
                   //    builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                   //    builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Error);
                   //})
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
                       var logger = sp.GetRequiredService<Serilog.ILogger>();
                       return new(mediator, defaultParams, logger);
                   })
                   .AddTransient(sp =>
                   {
                       var defLaserParams = ExtensionMethods
                            .DeserilizeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);
                       var mapper = sp.GetService<IMapper>();
                       var vm = mapper?.Map<MarkSettingsVM>(defLaserParams) ?? new();
                       return vm;
                   });
        }


        private async void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _principleLogger.ForContext<App>().Fatal(e.Exception, "An unhandled Exception was thrown");
            _principleLogger.ForContext<MicroProcess>().Fatal(e.Exception, RepoSink.Failed, RepoSink.App);
            //await _workTimeLogger.LogAppFailed(e.Exception);
        }
        private Serilog.ILogger _principleLogger;
        //private WorkTimeLogger _workTimeLogger;
        protected override async void OnStartup(StartupEventArgs e)//TODO Bad 
        {
            AllocConsole();

            var provider = MainIoC.BuildServiceProvider();
            //var loggerProvider = provider.GetRequiredService<ILoggerProvider>();
            //_principleLogger = loggerProvider.CreateLogger("AppLogger");
            //_workTimeLogger = provider.GetRequiredService<WorkTimeLogger>();
            //await _workTimeLogger.LogAppStarted(); 

            
            //_principleLogger = provider.GetRequiredService<Serilog.ILogger>();


            //_principleLogger.ForContext<MicroProcess>().Information(RepoSink.Start, RepoSink.App);

            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            var viewModel = provider.GetService<MainViewModel>();
            var context = provider.GetService<DbContext>() as LaserDbContext;
            var worktimeContext = provider.GetService<WorkTimeDbContext>();


            context?.LoadSetsAsync();
            
            /*
            Trace.TraceInformation("The application started");
            Trace.Flush();
            */
            var mainView = new MainView()
            {
                DataContext = viewModel
            };

            mainView.Closing += MainView_Closing;
            mainView.Show();

            viewModel?.OnInitialized();
            base.OnStartup(e);
        }

        private async void MainView_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
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
            //await _workTimeLogger.LogAppStopped();
            
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
        }
    }
}
