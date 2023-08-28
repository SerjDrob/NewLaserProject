using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.ViewModels.DialogVM;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class AppSettingsVM
    {
        public LaserEntity DefaultEntityType
        {
            get; set;
        }
        public Material DefaultMaterial
        {
            get; set;
        }
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
        public ObservableCollection<DefaultLayerEntityTechnology> DefaultTechnologies { get; set; } = new();
        public MarkLaserParams DefaultLaserParams
        {
            get; private set;
        }
        public ObservableCollection<DefaultLayerFilter> DefLayerFilters { get; set; } = new();
        public DefaultTechSelector DefaultTechSelector
        {
            get; set;
        }
        public ObservableCollection<DefaultTechSelector> DefaultTechSelectors
        {
            get; set;
        }
        public ObservableCollection<object> EntityTypes { get; set; } = new() { LaserEntity.Circle, LaserEntity.Curve };
        public string AddLayerName
        {
            get; set;
        }
        public bool AddLayerIsVisible
        {
            get; set;
        }
        public DefaultLayerFilter CurrentLayerFilter
        {
            get; set;
        }
        public LaserEntity CurrentEntityType
        {
            get; set;
        }
        public Technology CurrentTechnology
        {
            get; set;
        }
        public ObservableCollection<Material> Materials
        {
            get; set;
        }

        private readonly DbContext _db;

        public MarkSettingsVM MarkSettingsViewModel
        {
            get; set;
        }

        public AppSettingsVM(DbContext db, MarkLaserParams defaultLaserParams)
        {
            _db = db;

            _db.Set<Material>()
                .Include(m => m.Technologies)
                .Load();

            _db.Set<DefaultLayerEntityTechnology>()
               .Include(d => d.DefaultLayerFilter)
               .Load();

            _db.Set<DefaultLayerFilter>().Load();


            Materials = _db.Set<Material>()
                .Local
                .ToObservableCollection();

            DefaultTechnologies = _db.Set<DefaultLayerEntityTechnology>()
                .Local
                .ToObservableCollection();

            DefLayerFilters = _db.Set<DefaultLayerFilter>()
                .Local
                .ToObservableCollection();

            SetLayerFilters();
            SetDTO();

            DefaultTechnologies.CollectionChanged += DefaultTechnologies_CollectionChanged;

            DefaultLaserParams = defaultLaserParams;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MarkLaserParams, MarkSettingsVM>()
                .IncludeMembers(s => s.PenParams, s => s.HatchParams);
                cfg.CreateMap<PenParams, MarkSettingsVM>(MemberList.None);
                cfg.CreateMap<HatchParams, MarkSettingsVM>(MemberList.None);
            });

            var markParamsToMSVMMapper = config.CreateMapper();

            MarkSettingsViewModel = markParamsToMSVMMapper.Map<MarkSettingsVM>(defaultLaserParams);
        }

        private void DefaultTechnologies_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems?[0] is DefaultLayerEntityTechnology removed)
            {
                var filterMatch = DefaultTechSelector.DefLayerFilter.Filter == removed.DefaultLayerFilter.Filter;
                var entityMatch = DefaultEntityType == removed.EntityType;
                var materialMatch = DefaultMaterial == removed.Technology.Material;
                if (filterMatch && entityMatch && materialMatch)
                {
                    SetLayerFilters();
                }
            }
        }

        [ICommand]
        private void AddLayer()
        {
            if (AddLayerName != string.Empty)
            {
                var filter = AddLayerName.Trim();
                if (!DefLayerFilters.Where(d => d.Filter.Contains(filter, StringComparison.InvariantCultureIgnoreCase)).Any())
                {
                    var defaultLayerFilter = new DefaultLayerFilter
                    {
                        Filter = filter,
                        IsVisible = AddLayerIsVisible
                    };
                    _db.Add(defaultLayerFilter);
                    _db.SaveChanges();
                    AddLayerName = string.Empty;
                }
            }
        }
        
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
            _db.Add(defaultTechnology);
            _db.SaveChanges();
        }
        public void SaveDbChanges()
        {
            _db?.SaveChanges();
        }
        private void SetLayerFilters()
        {
            DefaultTechSelectors = DefaultTechnologies.GroupBy(d => d.DefaultLayerFilter, (k, col) =>
            new DefaultTechSelector(k, col.GroupBy(g => g.EntityType)
            .ToImmutableDictionary(k=>k.Key,e=>e.Select(g=>g.Technology.Material))
            )).ToObservableCollection();
        }
        private void SetDTO()
        {
            var defLayerProcDTO = ExtensionMethods.DeserilizeObject<DefaultProcessFilterDTO>(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS, "DefaultProcessFilter.json"));
            if (defLayerProcDTO is not null)
            {
                DefaultHeight = defLayerProcDTO.DefaultHeight;
                DefaultWidth = defLayerProcDTO.DefaultWidth;

                var defsel = DefaultTechSelectors?.SingleOrDefault(d => d.DefLayerFilter.Id == defLayerProcDTO.LayerFilterId);
                var defType = (LaserEntity)defLayerProcDTO.EntityType;

                if (defsel is not null && defsel.Entities.Contains(defType))
                {
                    if (defsel.EntMaterials.TryGetValue(defType, out var materials))
                    {
                        var defmaterial = Materials.SingleOrDefault(m => m.Id == defLayerProcDTO.MaterialId);
                        if (defmaterial is not null)
                        {
                            DefaultTechSelector = defsel;
                            DefaultEntityType = defType;
                            DefaultMaterial = defmaterial;
                        }
                    }
                }
            }
        }
    }
}
