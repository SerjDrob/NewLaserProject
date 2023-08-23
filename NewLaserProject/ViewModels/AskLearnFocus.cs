using System;
using System.ComponentModel;
using HandyControl.Tools.Extension;

namespace NewLaserProject.ViewModels
{
    internal class AskLearnFocus : IDialogResultable<AskLearnFocus>
    {
        [DisplayName("Начальная высота")]
        public double Height { get; set; }
        [DisplayName("Скорость")]
        public double Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                if (_speed < 1000)
                {
                    CloseAction?.Invoke();
                }
            }
        }
        private double _speed;
        public AskLearnFocus Result { get => this; set { } }
        public Action CloseAction { get; set; }
    }
}
