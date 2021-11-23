using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using System.Windows.Input;
using MachineClassLibrary.Laser;

namespace NewLaserProject.Classes
{
    [INotifyPropertyChanged]
    public partial class ProgModuleItemVM
    {
        public bool CanAcceptChildren { get; set; }
        public ChildrenObservCollection<ProgModuleItemVM> Children { get; private set; }
        public ProgModuleItemVM()
        {
            Children = new(this);
        }
        public void AddChild(ProgModuleItemVM child)
        {
            Children.Add(child);
        }
        public int LoopCount { get; set; }
        public double Tapper { get; set; }
        public double DeltaZ { get; set; }
        public int DelayTime { get; set; }
        public MarkLaserParams MarkParams { get; set; }
        public ModuleType ModuleType { get; set; }

        [ICommand]
        private void ClickButton()
        {
            var t = LoopCount;
        }
        public bool ShouldSerializeChildren()
        {
            return ModuleType == ModuleType.Loop;
        }
        public bool ShouldSerializeLoopCount()
        {
            return ModuleType == ModuleType.Loop;
        }
        public bool ShouldSerializeTapper()
        {
            return ModuleType == ModuleType.AddDiameter;
        }
        public bool ShouldSerializeClickButtonCommand() => false;
        public bool ShouldSerializeDelayTime()
        {
            return ModuleType == ModuleType.Delay;
        }
        public bool ShouldSerializeMarkParams()
        {
            return ModuleType == ModuleType.Pierce;
        }
       
    }
}
