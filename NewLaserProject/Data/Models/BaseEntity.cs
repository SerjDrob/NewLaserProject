using System.ComponentModel.DataAnnotations;

namespace NewLaserProject.Data.Models
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
