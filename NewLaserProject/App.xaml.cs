using AutoMapper;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Markers;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.Data;
using NewLaserProject.Repositories;
using NewLaserProject.ViewModels;
using NewLaserProject.ViewModels.DialogVM;
using NewLaserProject.ViewModels.DialogVM.Profiles;
using NewLaserProject.Views;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;
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
                .DeserilizeObject<MachineConfiguration>(Path.Combine(ProjectFolders.APP_SETTINGS, "MachineConfigs.json"))
                ?? throw new NullReferenceException("The machine configs are null");

            MainIoC = new ServiceCollection();

            _ = MainIoC.AddMediatR(cfg=>cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()))
                   .AddDbContext<DbContext, LaserDbContext>(options =>
                   {
                       options.UseSqlite(new SqlConnectionStringBuilder()
                       {
                           DataSource = Path.Join(ProjectFolders.DATA, "laserDatabase.db")
                       }.ToString());
                   }, ServiceLifetime.Singleton)
                   .AddTransient(typeof(IRepository<>), typeof(LaserRepository<>))
                   .AddSingleton<ISubject<IProcessNotify>, Subject<IProcessNotify>>()
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
                   .AddLogging(builder =>
                   {
                       builder.AddFile(ProjectPath.GetFilePathInFolder(ProjectFolders.TEMP_FILES, "app.log"));
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
                            .DeserilizeObject<MarkLaserParams>(Path.Combine(ProjectFolders.APP_SETTINGS, "DefaultLaserParams.json"));

                       var mapper = sp.GetService<IMapper>();
                       var mediator = sp.GetService<IMediator>();
                       var defaultParams = mapper?.Map<ExtendedParams>(defLaserParams);
                       return new(mediator, defaultParams);
                   })
                   .AddTransient<MarkSettingsVM>(sp =>
                   {
                       var defLaserParams = ExtensionMethods
                            .DeserilizeObject<MarkLaserParams>(Path.Combine(ProjectFolders.APP_SETTINGS, "DefaultLaserParams.json"));

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

#if PCIInserted


            var viewModel = provider.GetService<MainViewModel>();
            var context = provider.GetService<DbContext>() as LaserDbContext;
            context?.LoadSetsAsync();
#else
            var db = provider.GetService<LaserDb Context>();
            var mediator = provider.GetService<IMediator>();
            var viewModel = new MainViewModel(db,mediator);
#endif
            base.OnStartup(e);

            Trace.TraceInformation("The application started");
            Trace.Flush();

            new MainView()
            {
                DataContext = viewModel
            }.Show();

            viewModel?.OnInitialized();
        }
    }
}
