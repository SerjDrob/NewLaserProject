using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.WorkTimeFeatures.Get;
using Tang.Library.Common.Component.Extensions;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class WorkTimeStatisticsVM
    {
        private readonly IMediator _mediator;
        public ObservableCollection<WorkTimeLog>? WorkTimeLogs { get; set; }
        public WorkTimeStatisticsVM(IMediator mediator)
        {
            _mediator = mediator;
            _mediator.Send(new GetFullWorkTimeLogRequest())
                .ContinueWith(x =>
                {
                    WorkTimeLogs = x.Result?.Logs.Where(l=>l?.ProcTimeLogs.Any() ?? false).ToObservableCollection();

                }, TaskScheduler.Default).ConfigureAwait(false);
        }
    }
}
