using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Miscellaneous;
using MachineControlsLibrary.CommonDialog;
using MediatR;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes.ProgBlocks;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.Data.Models.MaterialEntRuleFeatures.Create;
using NewLaserProject.Data.Models.MaterialEntRuleFeatures.Get;
using NewLaserProject.Data.Models.MaterialEntRuleFeatures.Update;
using NewLaserProject.Data.Models.MaterialFeatures.Create;
using NewLaserProject.Data.Models.MaterialFeatures.Delete;
using NewLaserProject.Data.Models.MaterialFeatures.Get;
using NewLaserProject.Data.Models.MaterialFeatures.Update;
using NewLaserProject.Data.Models.TechnologyFeatures.Create;
using NewLaserProject.Data.Models.TechnologyFeatures.Delete;
using NewLaserProject.Data.Models.TechnologyFeatures.Update;
using NewLaserProject.ViewModels.DbVM;
using NewLaserProject.ViewModels.DialogVM;
using Newtonsoft.Json;
using PropertyChanged;
using MsgBox = HandyControl.Controls.MessageBox;
using WinDialogs = Microsoft.Win32;

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
        public LaserDbViewModel(IMediator mediator, ExtendedParams defaultParams, Serilog.ILogger logger)
        {
            _mediator = mediator;
            _logger = logger;// logger.CreateLogger("LaserDb");
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
        private readonly Serilog.ILogger _logger;
        private readonly ExtendedParams _defaultParams;

        private void ReviseTechnologies() => Technologies = Materials.SelectMany(m => m.Technologies).ToObservableCollection();

        [ICommand]
        private async Task AddMaterial()
        {
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Новый материал")
                .SetDataContext<MaterialVM>(vm => vm.MaterialDTO = new() { Thickness = 0.25f })
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
                    MsgBox.Error("Программа не содержит ни одного блока прошивки. Технология не будет сохранена.", "Технология");
                    return;
                }
                var newTechnology = new Technology();
                newTechnology.Material = material;
                newTechnology.ProgramName = writeTechVM.TechnologyName ?? DateTime.Now.ToString();
                newTechnology.ProcessingProgram = writeTechVM.SaveListingToFolder(AppPaths.TechnologyFolder, newTechnology.ProgramName, material.Name);
                newTechnology.Created = DateTime.Now;
                newTechnology.Altered = DateTime.Now;
                var response = await _mediator.Send(new CreateTechnologyRequest(newTechnology));
                Technologies.Add(response.CreatedTechnology);
                //ReviseTechnologies();
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
        private async Task EditTechnology(Technology technology) => await EditCopyTechnologyAsync(technology);

        private async Task EditCopyTechnologyAsync(Technology technology, bool copy = false)
        {
            var tech = copy ? (Technology)technology.Clone() : technology;
            var defParams = (ExtendedParams)_defaultParams.Clone();
            defParams.ContourOffset = tech.Material.MaterialEntRule?.Offset ?? 0;
            defParams.HatchWidth = tech.Material.MaterialEntRule?.Width ?? 0;

            var path = Path.Combine(AppPaths.TechnologyFolder, $"{tech.ProcessingProgram}.json");

            var writeTechVM = new WriteTechnologyVM(defParams)
            {
                EditEnable = true,
                TechnologyName = copy ? string.Empty : tech.ProgramName,
                MaterialName = tech.Material.Name,
                MaterialThickness = tech.Material.Thickness,
            };

            try
            {
                writeTechVM.LoadListing(path);
            }
            catch (Exception ex)
            {
                _logger.ForContext<LaserDbViewModel>().Error(ex, "The exception was thrown in the EditCopyTechnologyAsync method");
                MsgBox.Error("Файл программы не найден в базе.", "Технология");
                return;
            }

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle(copy ? "Создать из копии" : "Правка программы")
                .SetDataContext(writeTechVM, vm => { })
                .GetCommonResultAsync<TechWizardVM>()
                .ConfigureAwait(false);

            if (result.Success)
            {
                if (!SearchPierceBlock(result.CommonResult.Listing))
                {
                    MsgBox.Error("Программа не содержит ни одного блока прошивки. Технология не будет сохранена.", "Технология");
                    return;
                }
                if (copy)
                {
                    tech.Id = 0;
                    tech.Created = DateTime.Now;
                }
                else if (tech.Created == default)
                {
                    tech.Created = DateTime.Now;
                }
                tech.ProgramName = writeTechVM.TechnologyName;
                tech.Altered = DateTime.Now;
                tech.ProcessingProgram = result.CommonResult.SaveListingToFolder(AppPaths.TechnologyFolder, tech.ProgramName, tech.Material.Name);
                var response = copy ? await _mediator.Send(new CreateTechnologyRequest(tech)) : await _mediator.Send(new UpdateTechnologyRequest(tech));
                if (!copy)
                {
                    File.Delete(path);
                    technology = response.CreatedTechnology;
                    ReviseTechnologies();
                }
                if (copy) Technologies.Add(response.CreatedTechnology);
            }
        }

        [ICommand]
        private async void DeleteTechnology(Technology technology)
        {
            if (MsgBox.Ask($@"Удалить технологию ""{technology.ProgramName}"" ?", "Удаление") != System.Windows.MessageBoxResult.OK) return;
            var response = await _mediator.Send(new DeleteTechnologyRequest(technology));
            if (response.IsDeleted)
            {
                DeleteTechnologyFile(technology);
                Technologies.Remove(technology);
            }
            //ReviseTechnologies();
        }
        [ICommand]
        private async void CopyTechnology(Technology technology) => EditCopyTechnologyAsync(technology, true);

        private void DeleteTechnologyFile(Technology technology)
        {
            var path = Path.Combine(AppPaths.TechnologyFolder, $"{technology.ProcessingProgram}.json");
            File.Delete(path);
        }
        [ICommand]
        private async Task DeleteMaterial(Material material)
        {
            if (MsgBox.Ask($@"Удалить материал ""{material.Name}"" ? Вместе с материалом будут удалены все связанные с ним технологии.", "Удаление") != System.Windows.MessageBoxResult.OK) return;
            var request = new DeleteGetMaterialRequest(material);
            var response = await _mediator.Send(request);
            Materials = response.Materials.ToObservableCollection();
            Technologies = Materials.SelectMany(m => m.Technologies).ToObservableCollection();
            material.Technologies?.ForEach(DeleteTechnologyFile);
        }

        public string TechnologyFilter
        {
            get;
            set;
        } = string.Empty;

        [ICommand]
        private void FilterTechnology(FilterEventArgs filterEventArgs)
        {
            if (TechnologyFilter == string.Empty) return;
            if (filterEventArgs.Item is not Technology technology) return;
            var pattern = TechnologyFilter.Aggregate(string.Empty, (a, b) => a + b + @"\w*?");

            var reg = new Regex($"(?>{pattern})", RegexOptions.IgnoreCase | RegexOptions.Compiled);//(?>T\w*?)
            var sb = new StringBuilder();

            sb.Append(technology.ProgramName)
                .Append(technology.Material.Name)
                .Append(technology.Material.Thickness);

            var str = sb.ToString();
            str = Regex.Replace(str, @"\W", "");
            var result = reg.IsMatch(str);
            filterEventArgs.Accepted = result;
        }

        [ICommand]
        private void TypeFilter(object o)
        {
            var collection = o as DataGrid;
            if (collection is not null) CollectionViewSource.GetDefaultView(collection.ItemsSource).Refresh();
        }

        [ICommand]
        private async Task UploadDb()
        {
            var dialog = new WinDialogs.SaveFileDialog();
            dialog.Filter = "Laser database (*.ldb)|*.ldb";
            if (dialog.ShowDialog() ?? false)
            {
                var path = dialog.FileName;
                var materials = await _mediator.Send(new GetFullMaterialHasTechnologyRequest());
                try
                {
                    var techs = materials.Materials
                        .SelectMany(m => m.Technologies)
                        .Select(t =>
                        {
                            var path = Path.Combine(AppPaths.TechnologyFolder, $"{t.ProcessingProgram}.json");
                            var listing = File.ReadAllText(path);
                            return new { Listing = listing, Technology = t };
                        });
                    var json = JsonConvert.SerializeObject(techs, Formatting.Indented, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    });
                    File.WriteAllText(path, json);
                }
                catch (Exception)
                {

                    throw;
                }
            }


        }

        [ICommand]
        private async Task DownloadDb()
        {
            var dialog = new WinDialogs.OpenFileDialog();
            dialog.Filter = "Laser database (*.ldb)|*.ldb";
            if (dialog.ShowDialog() ?? false)
            {
                var result = await Dialog.Show<CommonDialog>()
                    .SetDialogTitle("Загрузка базы технологий")
                    .SetDataContext<DownloadDbVM>(vm => vm.DatabasePath = dialog.FileName)
                    .GetCommonResultAsync<DownloadDbVM>()
                    .ConfigureAwait(false);
                if (result.Success)
                {
                    var settings = result.CommonResult;
                    var json = File.ReadAllText(settings.DatabasePath);
                    List<T> CreateList<T>(params T[] items)
                    {
                        return new List<T>(items);
                    }
                    var technologies =
                        JsonConvert.DeserializeAnonymousType(json, CreateList(new { Listing = "", Technology = new Technology() }));

                    var ReviseDatabase = async (IEnumerable<Technology>? intersected, bool asNew = false) =>
                    {
                        Guard.IsNotNull(technologies, nameof(technologies));
                        var sourceTechs = intersected is not null ? technologies
                        .IntersectBy(intersected, s => s.Technology, new TechComparer()) : technologies;

                        var listings = sourceTechs
                            .Select(t => new { FileName = t.Technology.ProcessingProgram + ".json", Listing = t.Listing })
                            .ToList();

                        foreach (var listing in listings)
                        {
                            File.WriteAllText(Path.Combine(AppPaths.TechnologyFolder, listing.FileName), listing.Listing);
                        }


                        var notExistMaterialTechs = sourceTechs
                        .Where(t => !Materials.Any(m => m.Name == t.Technology.Material.Name))
                        .Select(t => t.Technology);

                        var existMaterialTechs = sourceTechs
                        .Where(t => Materials.Any(m => m.Name == t.Technology.Material.Name))
                        .Select(t => t.Technology);

                        if (notExistMaterialTechs?.Any() ?? false)
                        {
                            var materials = notExistMaterialTechs.Select(o => o.Material)
                                                .DistinctBy(m => m.Id)
                                                .Select(m =>
                                                {
                                                    if (m.MaterialEntRule is not null)
                                                    {
                                                        m.MaterialEntRule.Id = 0;
                                                        m.MaterialEntRule.MaterialId = 0;
                                                        m.MaterialEntRule.Material = null;
                                                    }
                                                    m.Technologies = sourceTechs.Select(t => t.Technology).Where(t => t.MaterialId == m.Id).ToList();
                                                    m.Id = 0;
                                                    return m;
                                                }).ToList();

                            materials.ForEach(m => m?.Technologies?.ForEach(t => t.Id = 0));
                            foreach (var material in materials)
                            {
                                await _mediator.Send(new CreateMaterialRequest(material));
                            }
                        }

                        var existedMaterials = Materials.Where(m => existMaterialTechs.Any(t => t.Material.Name == m.Name)).ToList();
                        if (existedMaterials.Any())
                        {
                            foreach (var material in existedMaterials)
                            {
                                var techs = existMaterialTechs.Where(t => t.Material.Name == material.Name)
                                .Select(t =>
                                {
                                    t.Id = 0;
                                    t.MaterialId = 0;
                                    t.Material = material;
                                    if (asNew) t.ProgramName += "-new";
                                    return t;
                                }).ToList();
                                material.Technologies ??= new();
                                material.Technologies.AddRange(techs);
                                var updatedMaterial = await _mediator.Send(new UpdateMaterialRequest(material));
                            }
                        }


                        var request = new GetFullMaterialRequest();
                        var response = await _mediator.Send(request);
                        Materials = response.Materials.ToObservableCollection();
                        ReviseTechnologies();
                    };

                    if (settings.RewriteDatabase && technologies is not null)
                    {
                        try
                        {

                            var deleted = await _mediator.Send(new DeleteFullMaterialRequest());
                            if (deleted.success)
                            {
                                Materials = new();
                                Technologies = new();
                                var di = new DirectoryInfo(AppPaths.TechnologyFolder);
                                foreach (var fileInfo in di.EnumerateFiles())
                                {
                                    fileInfo.Delete();
                                }
                            }
                            else throw new InvalidOperationException("Can not delete the database");
                            await ReviseDatabase(null);
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }
                    else if (!settings.RewriteDatabase && technologies is not null)
                    {
                        var deleteTechnology = async (Technology technology) =>
                        {
                            var response = await _mediator.Send(new DeleteTechnologyRequest(technology));
                            if (response.IsDeleted)
                            {
                                DeleteTechnologyFile(technology);
                            }
                        };

                        var currentTechnologies = Materials.SelectMany(m => m.Technologies).ToList();
                        var downloadedTechs = technologies.Select(t => t.Technology).ToList();
                        var resultCollection = new List<Technology>();
                        if (settings.MergeChangeOnNew)
                        {
                            var difference = currentTechnologies.Except(downloadedTechs, new TechComparer());
                            var forDeleting = currentTechnologies.Except(difference, new TechComparer());
                            foreach (var t in forDeleting)
                            {
                                await deleteTechnology(t);
                            }
                            await ReviseDatabase(null);
                        }
                        else if (settings.MergeNotSave)
                        {
                            var difference = downloadedTechs.Except(currentTechnologies, new TechComparer());
                            await ReviseDatabase(difference);
                        }
                        else if (settings.MergeSaveBoth)
                        {
                            var difference = downloadedTechs.Except(currentTechnologies, new TechComparer());
                            var same = downloadedTechs.Except(difference, new TechComparer());
                            await ReviseDatabase(same, true);
                            await ReviseDatabase(difference);
                        }
                    }
                }
            }
        }

        class TechComparer : IEqualityComparer<Technology>
        {
            public bool Equals(Technology? x, Technology? y)
            {
                if (x is null || y is null) return false;
                if (x.ProgramName.Equals(y.ProgramName))
                {
                    return x.Material.Name == y.Material.Name;
                }
                return false;
            }

            public int GetHashCode([DisallowNull] Technology obj)
            {
                return new HashCode().ToHashCode();
            }
        }
    }
}
