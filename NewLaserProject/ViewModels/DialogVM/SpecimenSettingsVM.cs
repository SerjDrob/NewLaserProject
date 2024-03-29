﻿using System.Collections.ObjectModel;
using MachineClassLibrary.Laser.Entities;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NewLaserProject.Data.Models;

namespace NewLaserProject.ViewModels.DialogVM
{
    [INotifyPropertyChanged]
    public partial class SpecimenSettingsVM : CommonDialogResultable<SpecimenSettingsVM>
    {
        public ObservableCollection<DefaultTechSelector> DefaultTechSelectors
        {
            get; set;
        }
        public ObservableCollection<DefaultLayerEntityTechnology> DefaultTechnologies { get; set; } = new();
        public int DefaultWidth
        {
            get; set;
        }
        public int DefaultHeight
        {
            get; set;
        }
        public bool IsMirrored
        {
            get; set;
        }
        public bool IsRotated
        {
            get; set;
        }
        public LaserEntity DefaultEntityType
        {
            get; set;
        }
        public Material DefaultMaterial
        {
            get; set;
        }
        public DefaultTechSelector DefaultTechSelector
        {
            get; set;
        }

        public override void SetResult() => SetResult(this);
    }
}
