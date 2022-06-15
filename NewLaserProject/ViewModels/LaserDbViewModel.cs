﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Data;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.ViewModels.DbVM;
using NewLaserProject.Views;
using NewLaserProject.Views.DbViews;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace NewLaserProject.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public partial class LaserDbViewModel
    {
        public ObservableCollection<Technology> Technologies { get; set; }// = new();
        public ObservableCollection<Material> Materials { get; set; }// = new();

        public LaserDbViewModel(DbContext db)
        {
            _db = db;

            _db.Set<Technology>()
                .Include(t => t.Material)
                .LoadAsync()
                //TODO: fix it!
                .ContinueWith(_ => Technologies = _db.Set<Technology>().Local.ToObservableCollection())
                .ConfigureAwait(false);

            _db.Set<Material>()
                .LoadAsync()
                .ContinueWith(_ => Materials = _db.Set<Material>().Local.ToObservableCollection())
                .ConfigureAwait(false);

        }
        private readonly DbContext _db;

        [ICommand]
        private void AddMaterial()
        {
            var material = new MaterialDTO();
            var result = new AddToDbView
            {
                Title = "Материал",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = false,
                ShowInTaskbar = false,
                DataContext = material
            }.ShowDialog();

            if (result ?? false)
            {
                var newMaterial = new Material(material);
                _db.Add(newMaterial);
                _db.SaveChanges();
            }
        }

        [ICommand]
        private void AssignTechnology(Material material)
        {
            var writeTechVM = new WriteTechnologyVM
            {
                MaterialName = material.Name,
                MaterialThickness = material.Thickness
            };
            var result = new AddToDbContainerView
            {
                DataContext = writeTechVM
            }.ShowDialog();
            if (result ?? false)
            {
                
                var newTechnology = new Technology();
                newTechnology.Material = material;

                var path = ProjectPath.GetFolderPath("TechnologyFiles");
                newTechnology.ProcessingProgram = writeTechVM.TechnologyWizard.SaveListingToFolder(path);

                newTechnology.ProgramName = writeTechVM.TechnologyName;
                _db.Set<Technology>()
                          .Add(newTechnology);
                _db.SaveChanges();
            }
        }

        [ICommand]
        private void EditTechnology(Technology technology)
        {
            var techWizard = new TechWizardViewModel { EditEnable = true };
            
            var workingDirectory = Environment.CurrentDirectory;
            var projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            var path = Path.Combine(projectDirectory, "TechnologyFiles",$"{technology.ProcessingProgram}.json");
            techWizard.LoadListing(path);

            var result = new AddToDbContainerView
            {
                DataContext = techWizard
            }.ShowDialog();
            if (result ?? false)
            {
                technology.ProcessingProgram = techWizard.SaveListingToFolder(Path.Combine(projectDirectory, "TechnologyFiles"));
                _db.Set<Technology>().Update(technology);
                _db.SaveChanges();
                File.Delete(path);
            }
        }
        [ICommand]
        private void ViewTechnology(Technology technology)
        {
            var techWizard = new TechWizardViewModel { EditEnable = false };

            var workingDirectory = Environment.CurrentDirectory;
            var projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            var path = Path.Combine(projectDirectory, "TechnologyFiles", $"{technology.ProcessingProgram}.json");
            techWizard.LoadListing(path);

            new AddToDbContainerView(false)
            {
                DataContext = techWizard
            }.ShowDialog();            
        }
        [ICommand]
        private void DeleteTechnology(Technology technology)
        {            
            _db.Set<Technology>()
                .Remove(technology);
            DeleteTechnologyFile(technology);
            _db.SaveChanges();            
        }
        private void DeleteTechnologyFile(Technology technology)
        {
            var path = Path.Combine(ProjectPath.GetFolderPath("TechnologyFiles"), $"{technology.ProcessingProgram}.json");
            File.Delete(path);
        }
        [ICommand]
        private void DeleteMaterial(Material material)
        {
            var deletingMaterial = _db.Remove(material);
            if (deletingMaterial is not null)
            {
                deletingMaterial
                    .Entity?
                    .Technologies?
                    .ForEach(t=>DeleteTechnologyFile(t));
            }
            _db.SaveChanges();
        }
    }
}