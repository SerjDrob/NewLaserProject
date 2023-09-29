using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Laser.Parameters;
using MediatR;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.Data.Models.MaterialEntRuleFeatures.Create;
using NewLaserProject.Data.Models.MaterialEntRuleFeatures.Get;
using NewLaserProject.Data.Models.MaterialEntRuleFeatures.Update;
using NewLaserProject.Data.Models.MaterialFeatures.Create;
using NewLaserProject.Data.Models.MaterialFeatures.Delete;
using NewLaserProject.Data.Models.MaterialFeatures.Get;
using NewLaserProject.Data.Models.TechnologyFeatures.Create;
using NewLaserProject.Data.Models.TechnologyFeatures.Delete;
using NewLaserProject.Data.Models.TechnologyFeatures.Update;
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
        public LaserDbViewModel(IMediator mediator, ExtendedParams defaultParams)
        {
            _mediator = mediator;
            _defaultParams = defaultParams;
            _mediator.Send(new GetFullMaterialRequest())
                .ContinueWith(t =>
                {
                    var materialsRequest = t.Result;
                    Materials = materialsRequest.Materials.ToObservableCollection();
                    ReviseTechnologies();
                }, TaskScheduler.Default)
                .ConfigureAwait(false);
        }
        private readonly IMediator _mediator;
        private readonly ExtendedParams _defaultParams;

        private void ReviseTechnologies() => Technologies = Materials.SelectMany(m => m.Technologies).ToObservableCollection();

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

                var request = new CreateGetMaterialRequest(newMaterial);
                var response = await _mediator.Send(request);
                Materials = response.Materials.ToObservableCollection();
            }
        }

        private bool SearchPierceBlock(IEnumerable<IProgBlock> blocks)
        {
            if (blocks.OfType<PierceBlock>().Any()) return true;
            foreach (var block in blocks.OfType<LoopBlock>()) if (SearchPierceBlock(block.Children)) return true;
            return false;
        }
        [ICommand]
        private async Task AssignTechnology(Material material)
        {
            var defParams = (ExtendedParams)_defaultParams.Clone();
            defParams.ContourOffset = material.MaterialEntRule?.Offset ?? 0;
            defParams.HatchWidth = material.MaterialEntRule?.Width ?? 0;
            var writeTechVM = new WriteTechnologyVM(defParams)
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
                if (!SearchPierceBlock(result.CommonResult.Listing))
                {
                    MessageBox.Error("Программа не содержит ни одного блока прошивки. Технология не будет сохранена.", "Технология");
                    return;
                }
                var newTechnology = new Technology();
                newTechnology.Material = material;
                newTechnology.ProcessingProgram = writeTechVM.TechnologyWizard.SaveListingToFolder(AppPaths.TechnologyFolder);
                newTechnology.ProgramName = writeTechVM.TechnologyName ?? DateTime.Now.ToString();
                var response = await _mediator.Send(new CreateTechnologyRequest(newTechnology));
                ReviseTechnologies();
            }
        }
        [ICommand]
        private async void AssignRule(Material material)
        {
            var response = await _mediator.Send(new GetRuleByMaterialIdRequest(material.Id));
            var matEntRule = response.MaterialEntRule;

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Правило обработки")
                .SetDataContext<MaterialEntRuleVM>(vm => vm.MaterialEntRule = matEntRule ?? new MaterialEntRule
                {
                    Material = material,
                })
                .GetCommonResultAsync<MaterialEntRule>();

            if (result.Success)
            {
                if (matEntRule is not null)
                {
                    await _mediator.Send(new UpdateMaterialEntRuleRequest(result.CommonResult)).ConfigureAwait(false);
                }
                else
                {
                    await _mediator.Send(new CreateMaterialEntRuleRequest(result.CommonResult)).ConfigureAwait(false);
                }
            }
        }
        [ICommand]
        private async Task EditTechnology(Technology technology)
        {
            var defParams = (ExtendedParams)_defaultParams.Clone();
            defParams.ContourOffset = technology.Material.MaterialEntRule?.Offset ?? 0;
            defParams.HatchWidth = technology.Material.MaterialEntRule?.Width ?? 0;

            var techWizard = new TechWizardVM(defParams) { EditEnable = true };
            var path = Path.Combine(AppPaths.TechnologyFolder, $"{technology.ProcessingProgram}.json");
            techWizard.LoadListing(path);
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Правка программы")
                .SetDataContext(new WriteEditTechnologyVM(techWizard), vm => { })
                .GetCommonResultAsync<TechWizardVM>();

            if (result.Success)
            {
                if (!SearchPierceBlock(result.CommonResult.Listing))
                {
                    MessageBox.Error("Программа не содержит ни одного блока прошивки. Технология не будет сохранена.", "Технология");
                    return;
                }
                technology.ProcessingProgram = result.CommonResult.SaveListingToFolder(AppPaths.TechnologyFolder);
                var response = await _mediator.Send(new UpdateTechnologyRequest(technology));
                File.Delete(path);
            }
        }
        
        [ICommand]
        private async void DeleteTechnology(Technology technology)
        {
            var response = await _mediator.Send(new DeleteTechnologyRequest(technology));
            if (response.IsDeleted) DeleteTechnologyFile(technology);
            ReviseTechnologies();
        }
        private void DeleteTechnologyFile(Technology technology)
        {
            var path = Path.Combine(AppPaths.TechnologyFolder, $"{technology.ProcessingProgram}.json");
            File.Delete(path);
        }
        [ICommand]
        private async Task DeleteMaterial(Material material)
        {
            var request = new DeleteGetMaterialRequest(material);
            var response = await _mediator.Send(request);
            Materials = response.Materials.ToObservableCollection();
            Technologies = Materials.SelectMany(m => m.Technologies).ToObservableCollection();
            material.Technologies?.ForEach(DeleteTechnologyFile);
        }
    }
}
