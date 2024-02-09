using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace NewLaserProject.Data.Models
{
    public class WorkTimeLog : BaseEntity
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [NotMapped]
        public TimeSpan Duration { get => EndTime - StartTime; }
        [NotMapped]
        public TimeSpan ProcsTime { get => ProcTimeLogs?.Aggregate(new TimeSpan(), (acc, pl) => acc + pl.Duration) ?? new TimeSpan(); }
        [NotMapped]
        public double WorkLoad { get => 100 * ProcsTime / Duration; }
        public string? ExceptionMessage { get; set; }
        public List<ProcTimeLog>? ProcTimeLogs { get; set; }
    }
}
