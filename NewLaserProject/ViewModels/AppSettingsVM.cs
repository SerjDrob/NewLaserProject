using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class AppSettingsVM
    {
        public string AddLayerName { get; set; }
        public ObservableCollection<string> LayerMasks { get; set; } = new();
        
        [ICommand]
        private void AddLayer()
        {
            if (AddLayerName != string.Empty)
            {
                LayerMasks.Add(AddLayerName);
                AddLayerName = string.Empty; 
            }
        }
    }
}
