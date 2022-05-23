using Microsoft.EntityFrameworkCore;
using NewLaserProject.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Data
{
    public class LaserDbContext : DbContext
    {        
        public LaserDbContext(DbContextOptions<LaserDbContext> options) : base(options)
        {
        }
        public DbSet<Material> Material { get; set; }
        public DbSet<Technology> Technology { get; set; }
        
    }
}
