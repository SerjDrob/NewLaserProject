using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MachineClassLibrary.Laser.Entities;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Data.Models;

namespace NewLaserProject.ViewModels.DialogVM
{

    [INotifyPropertyChanged]
    internal partial class FileViewDialogVM : CommonDialogResultable<IEnumerable<DefaultLayerFilter>>
    {
        public ObservableCollection<DefaultLayerFilter> DefLayerFilters
        {
            get; set;
        }
        public ObservableCollection<Material> Materials
        {
            get; set;
        }
        public string AddLayerName
        {
            get; set;
        }
        public bool AddLayerIsVisible
        {
            get; set;
        }
        public LaserEntity CurrentEntityType { get; set; } = LaserEntity.Circle;
        public DefaultLayerFilter CurrentLayerFilter
        {
            get; set;
        }
        public Technology CurrentTechnology
        {
            get; set;
        }
        public ObservableCollection<DefaultLayerEntityTechnology> DefaultTechnologies { get; set; } = new();


        public override void SetResult() => SetResult(DefLayerFilters);

        [ICommand]
        private void AddLayer()
        {
            if (AddLayerName is null or "") return;
            var filter = AddLayerName.Trim();
            if (!DefLayerFilters.Where(d => d.Filter.Contains(filter, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                var defaultLayerFilter = new DefaultLayerFilter
                {
                    Filter = filter,
                    IsVisible = AddLayerIsVisible
                };
                DefLayerFilters.Add(defaultLayerFilter);
                AddLayerName = string.Empty;
            }
        }
        [ICommand]
        private void RemoveDefLayerFilter(DefaultLayerFilter defaultLayerFilter) => DefLayerFilters?.Remove(defaultLayerFilter);

        [ICommand]
        private void AddDefaultTechnology()
        {
            if (DefaultTechnologies.Where(d => d.DefaultLayerFilter.Filter == CurrentLayerFilter.Filter
                   && d.EntityType == CurrentEntityType
                   && d.Technology.Material == CurrentTechnology.Material)
                       .Any()) return;

            var defaultTechnology = new DefaultLayerEntityTechnology
            {
                DefaultLayerFilter = CurrentLayerFilter,
                Technology = CurrentTechnology,
                EntityType = CurrentEntityType
            };

            DefaultTechnologies.Add(defaultTechnology);
        }
        [ICommand]
        private void RemoveDefTechnology(DefaultLayerEntityTechnology defaultLayerEntityTechnology) => DefaultTechnologies?.Remove(defaultLayerEntityTechnology);
    }
}