using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HandyControl.Tools.Extension;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Data.Models;

namespace NewLaserProject.ViewModels.DialogVM;

[INotifyPropertyChanged]
internal partial class FileViewDialogVM : IDialogResultable<IEnumerable<DefaultLayerFilter>>
{
    public ObservableCollection<DefaultLayerFilter> DefLayerFilters { get; set; }
    public string AddLayerName
    {
        get; set;
    }
    public bool AddLayerIsVisible
    {
        get; set;
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
                DefLayerFilters.Add(defaultLayerFilter);
                AddLayerName = string.Empty;
            }
        }
    }
    [ICommand]
    private void RemoveDefLayerFilter(DefaultLayerFilter defaultLayerFilter) => DefLayerFilters?.Remove(defaultLayerFilter);
    public IEnumerable<DefaultLayerFilter> Result
    {
        get => DefLayerFilters;
        set
        {
        }
    }
    public Action CloseAction
    {
        get;
        set;
    }
}
