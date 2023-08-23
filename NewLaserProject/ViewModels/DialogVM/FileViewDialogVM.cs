using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Data.Models;

namespace NewLaserProject.ViewModels.DialogVM;

[INotifyPropertyChanged]
internal partial class FileViewDialogVM : CommonDialogResultable<IEnumerable<DefaultLayerFilter>>
{
    public ObservableCollection<DefaultLayerFilter> DefLayerFilters
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

    public override void SetResult() => SetResult(DefLayerFilters);

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
                DefLayerFilters.Add(defaultLayerFilter);
                AddLayerName = string.Empty;
            }
        }
    }
    [ICommand]
    private void RemoveDefLayerFilter(DefaultLayerFilter defaultLayerFilter) => DefLayerFilters?.Remove(defaultLayerFilter);

}
