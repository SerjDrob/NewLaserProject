using System;
using Microsoft.Extensions.DependencyInjection;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Markers;

namespace NewLaserProject.Classes
{
    internal class LaserBoardFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MachineConfiguration _machineConfiguration;

        public LaserBoardFactory(IServiceProvider serviceProvider, MachineConfiguration machineConfiguration)
        {
            _serviceProvider = serviceProvider;
            _machineConfiguration = machineConfiguration;
        }
        public IMarkLaser? GetLaserBoard()
        {
            if (_machineConfiguration.IsUF) return _serviceProvider.GetService<JCZLaser>();
            if (_machineConfiguration.IsIR) return _serviceProvider.GetService<JCZLaser>();
            if (_machineConfiguration.IsLaserMock) return _serviceProvider.GetService<MockLaser>();
            return null;
        }
    }
}
