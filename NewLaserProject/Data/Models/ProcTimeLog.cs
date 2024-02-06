using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewLaserProject.Data.Models
{
    public class ProcTimeLog : BaseEntity
    {
        public string? FileName { get; set; }
        public string? MaterialName { get; set; }
        public string? TechnologyName { get; set; }
        public double MaterialThickness { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [NotMapped]
        public TimeSpan Duration { get => EndTime - StartTime; }
        public TimeSpan YieldTime { get; set; }
        public bool Success { get; set; }
        public string? ExceptionMessage { get; set; }
        public WorkTimeLog WorkTimeLog { get; set; }
    }
}
