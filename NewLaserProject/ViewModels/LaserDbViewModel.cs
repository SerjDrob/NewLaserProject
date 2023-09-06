using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Laser.Parameters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.ViewModels.DbVM;
using NewLaserProject.ViewModels.DialogVM;
using NewLaserProject.Views.DbViews;
using NewLaserProject.Views.Dialogs;
using PropertyChanged;

namespace NewLaserProject.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public partial class LaserDbViewModel
    {
        public ObservableCollection<Technology> Technologies
        {
            get; set;
        }
        public ObservableCollection<Material> Materials
        {
            get; set;
        }
        public LaserDbViewModel(DbContext db, ExtendedParams defaultParams)
        {
            _db = db;
            _defaultParams = defaultParams;
            _db.Set<Technology>()
                .Include(t => t.Material)
                .LoadAsync()
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        Technologies = _db.Set<Technology>()
                            .Local
                            .ToObservableCollection();

                        _db.Set<Material>()
                            .Load();

                        Materials = _db.Set<Material>()
                            .Local
                            .ToObservableCollection();
                    }
                });
        }
        private readonly DbContext _db;
        private readonly ExtendedParams _defaultParams;

        [ICommand]
        private async Task AddMaterial()
        {
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Новый материал")
                .SetDataContext<MaterialVM>(vm => vm.MaterialDTO = new())
                .GetCommonResultAsync<MaterialDTO>();

            if (result.Success)
            {
                var newMaterial = new Material(result.CommonResult);
                _db.Add(newMaterial);
                _db.SaveChanges();
            }
        }

        [ICommand]
        private async Task AssignTechnology(Material material)
        {
            var writeTechVM = new WriteTechnologyVM(_defaultParams)
            {
                MaterialName = material.Name,
                MaterialThickness = material.Thickness
            };

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Новая технология")
                .SetDataContext(writeTechVM, vm => { })
                .GetCommonResultAsync<TechWizardVM>();

            if (result.Success)
            {
                var newTechnology = new Technology();
                newTechnology.Material = material;

                var path = ProjectPath.GetFolderPath(ProjectFolders.TECHNOLOGY_FILES);
                newTechnology.ProcessingProgram = writeTechVM.TechnologyWizard.SaveListingToFolder(path);

                newTechnology.ProgramName = writeTechVM.TechnologyName ?? DateTime.Now.ToString();//TODO if name isn't typed
                _db.Add(newTechnology);
                _db.SaveChanges();
            }
        }

        [ICommand]
        private async void AssignRule(Material material)
        {
            var matEntRule = _db.Set<MaterialEntRule>()
                    .AsNoTracking()
                    .SingleOrDefault(mer => mer.Material.Id == material.Id);

            var newRule = matEntRule ?? new MaterialEntRule
            {
                Material = material,
            };

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Правило обработки")
                .SetDataContext<MaterialEntRuleVM>(vm => vm.MaterialEntRule = newRule)
                .GetCommonResultAsync<MaterialEntRule>();

            if (result.Success)
            {
                if (matEntRule is not null)
                {
                    //_db.Set<MaterialEntRule>()
                    //    .Update(matEntRule);

                    var rule = _db.Set<MaterialEntRule>()
                    .SingleOrDefault(mer => mer.Material.Id == material.Id);
                    rule.Offset = matEntRule.Offset;
                    rule.Width = matEntRule.Width;
                }
                else
                {
                    _db.Set<MaterialEntRule>()
                        .Add(newRule);
                }
                _db.SaveChanges();
            }
        }

        [ICommand]
        private async Task EditTechnology(Technology technology)
        {
            var techWizard = new TechWizardVM(_defaultParams) { EditEnable = true };
            var path = ProjectPath.GetFilePathInFolder(ProjectFolders.TECHNOLOGY_FILES, $"{technology.ProcessingProgram}.json");
            techWizard.LoadListing(path);
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Правка программы")
                .SetDataContext(new WriteEditTechnologyVM(techWizard), vm => { })
                .GetCommonResultAsync<TechWizardVM>();

            if (result.Success)
            {
                technology.ProcessingProgram = result.CommonResult.SaveListingToFolder(ProjectPath.GetFolderPath(ProjectFolders.TECHNOLOGY_FILES));
                _db.Set<Technology>().Update(technology);
                _db.SaveChanges();
                File.Delete(path);
            }
        }
        [ICommand]
        private void ViewTechnology(Technology technology)
        {
            var techWizard = new TechWizardVM(_defaultParams) { EditEnable = false };
            var path = ProjectPath.GetFilePathInFolder(ProjectFolders.TECHNOLOGY_FILES, $"{technology.ProcessingProgram}.json");
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
            var path = ProjectPath.GetFilePathInFolder(ProjectFolders.TECHNOLOGY_FILES, $"{technology.ProcessingProgram}.json");
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
                    .ForEach(t => DeleteTechnologyFile(t));
            }
            _db.SaveChanges();
        }
    }
}
