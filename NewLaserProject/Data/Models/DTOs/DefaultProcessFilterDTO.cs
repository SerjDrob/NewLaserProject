using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Data.Models.DTOs
{
    internal class DefaultProcessFilterDTO
    {
        public int LayerFilterId { get; set; }
        public int MaterialId { get; set; }
        public uint EntityType { get; set; }
        public int DefaultWidth { get; set; }
        public int DefaultHeight { get; set; }
    }
}
