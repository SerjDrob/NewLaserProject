using MachineClassLibrary.Laser.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Data;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using PropertyChanged;
using AutoMapper;
using LaserLib = MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Parameters;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class AppSettingsVM
    {
        public string AddLayerName { get; set; }
        public bool AddLayerIsVisible { get; set; }
        public DefaultLayerFilter CurrentLayerFilter { get; set; }
        public LaserEntity CurrentEntityType { get; set; }
        public Technology CurrentTechnology { get; set; }
        public Material CurrentMaterial { get; set; }
       
        [OnChangedMethod(nameof(SetMaterials))]    
        public LaserEntity DefaultEntityType { get; set; }
        
        public int DefEntTypeIndex { get; set; } = 0;
        public Material? DefaultMaterial { get; set; }
        public DefaultLayerFilter? DefaultLayerFilter { get; set; }
       
        [OnChangedMethod(nameof(SetEntities))]
        public DefaultLayerEntityTechnology DefLayerEntTechnology { get; set; }
        
        public int DefaultWidth { get; set; }
        public int DefaultHeight { get; set; }
        public ObservableCollection<Material> Materials { get; set; }
        public ObservableCollection<Material> DefMaterials { get; set; }
        public ObservableCollection<object> EntityTypes { get; set; } = new() { LaserEntity.Circle, LaserEntity.Curve };
        public ObservableCollection<object> DefEntities { get; set; }
        public ObservableCollection<DefaultLayerEntityTechnology> DefaultTechnologies { get; set; } = new(); 
        public ObservableCollection<DefaultLayerFilter> LayerFilters { get; set; } = new();

        private readonly DbContext _db;

        public MarkLaserParams DefaultLaserParams { get; private set; }
        public MarkSettingsViewModel MarkSettingsViewModel { get; set; }

        public AppSettingsVM(DbContext db, MarkLaserParams defaultLaserParams)
        {
            _db = db;

            _db.Set<Material>()
                .Include(m => m.Technologies)
                .Load();
            Materials = _db.Set<Material>().Local.ToObservableCollection();

            _db.Set<DefaultLayerEntityTechnology>()
                .Load();
            DefaultTechnologies = _db.Set<DefaultLayerEntityTechnology>().Local.ToObservableCollection();

            _db.Set<DefaultLayerFilter>()
                .Load();
            LayerFilters = _db.Set<DefaultLayerFilter>().Local.ToObservableCollection();

            var defLayerProcDTO = ExtensionMethods.DeserilizeObject<DefaultProcessFilterDTO>(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "DefaultProcessFilter.json"));
            if (defLayerProcDTO is not null)
            {
                DefLayerEntTechnology = DefaultTechnologies.ToList()
                           .Where(d => d.DefaultLayerFilterId == defLayerProcDTO.LayerFilterId
                           && d.EntityType == (LaserEntity)defLayerProcDTO.EntityType
                           && d.Technology.MaterialId == defLayerProcDTO.MaterialId)
                           .Single();


                //DefaultLayerFilter = LayerFilters?.FirstOrDefault(d => d.Id == defLayerProcDTO.LayerFilterId, null);
                var defType = (LaserEntity)defLayerProcDTO.EntityType;
                SetEntities();
                SetMaterials();
                DefaultHeight = defLayerProcDTO.DefaultHeight;
                DefaultWidth = defLayerProcDTO.DefaultWidth;

                DefEntTypeIndex = EntityTypes.IndexOf(defType);
                DefaultMaterial = Materials?.FirstOrDefault(m => m.Id == defLayerProcDTO.MaterialId, null);
            }
            DefaultLaserParams = defaultLaserParams;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MarkLaserParams, MarkSettingsViewModel>()
                .IncludeMembers(s => s.PenParams, s => s.HatchParams);
                cfg.CreateMap<PenParams, MarkSettingsViewModel>(MemberList.None);
                cfg.CreateMap<HatchParams, MarkSettingsViewModel>(MemberList.None);

            });
            
            var markParamsToMSVMMapper = config.CreateMapper();

            MarkSettingsViewModel = markParamsToMSVMMapper.Map<MarkSettingsViewModel>(defaultLaserParams);
        }

        [ICommand]
        private void AddLayer()
        {
            if (AddLayerName != string.Empty)
            {
                var filter = AddLayerName.Trim();
                if (!LayerFilters.Where(d => d.Filter.Contains(filter, StringComparison.InvariantCultureIgnoreCase)).Any())
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
            if(DefaultTechnologies.Where(d => d.DefaultLayerFilter.Filter == CurrentLayerFilter.Filter 
            && d.EntityType == CurrentEntityType
            && d.Technology.Material==CurrentTechnology.Material)
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

                
        private void SetEntities()
        {
            DefEntities = DefaultTechnologies.Where(d=>d.DefaultLayerFilter.Filter == DefLayerEntTechnology.DefaultLayerFilter.Filter)
                .Select(d => (object)d.EntityType).ToObservableCollection();

            SetMaterials();
        }

        private void SetMaterials()
        {
            DefMaterials = DefaultTechnologies.Where(d => d.DefaultLayerFilter.Filter == DefLayerEntTechnology.DefaultLayerFilter.Filter
                                        && d.EntityType == DefaultEntityType)
                .Select(d => d.Technology.Material)
                .ToObservableCollection();
        }
    }
}
