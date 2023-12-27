using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    internal class LightColumn
    {
        public LightColumn(TimeSpan blinkTime, TimeSpan offTime)
        {
            _blinkTime = blinkTime;
            _offTime = offTime;
        }

        public LightColumn()
        {
            _blinkTime = TimeSpan.FromSeconds(1);
            _offTime = TimeSpan.FromSeconds(2);
        }

        private Dictionary<Light, (Action on, Action off)> _lights = new();
        private bool _isBlinking;
        private readonly TimeSpan _blinkTime;
        private readonly TimeSpan _offTime;

        public void AddLight(Light light, Action turnOn, Action turnOff)
        {
            _lights[light] = (turnOn, turnOff);
        }
        public void TurnOnLight(Light light)
        {
            _lights.Values.ToList().ForEach(l => l.off?.Invoke());
            _lights.TryGetValue(light, out var actions);
            actions.on?.Invoke(); 
        }

        public async Task BlinkLightAsync(Light light, CancellationToken cancellationToken = default)
        {
            TurnOff();
            if(cancellationToken == default)
            {
                var source = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                cancellationToken = source.Token;
            }
            _isBlinking = true;
            _lights.TryGetValue(light, out var actions);
            while (!cancellationToken.IsCancellationRequested & _isBlinking)
            {
                TurnOnLight(light);
                await Task.Delay(_blinkTime);
                actions.off?.Invoke();
                await Task.Delay(_offTime);
            }
        }
        public void TurnOff() 
        {
            _isBlinking = false;
            _lights.Values.ToList().ForEach(l => l.off?.Invoke()); 
        }
        public enum Light
        {
            Red,
            Yellow,
            Blue, 
            Green, 
        }
    }
}
