using Microsoft.EntityFrameworkCore;
using NewLaserProject.Data.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Data
{
    public class LaserDbContext : DbContext
    {
        //public LaserDbContext(DbContextOptions<LaserDbContext> options) : base(options)
        //{
        //}
        public LaserDbContext()
        {

            DbPath = Path.Join( ProjectPath.GetFolderPath("Data"), "laserDatabase.db");
            
        }

        public string DbPath { get; }
        public DbSet<Material> Material { get; set; }
        public DbSet<Technology> Technology { get; set; }
        public DbSet<DefaultLayerEntityTechnology> DefaultLayerEntityTechnology { get; set; }
        public DbSet<DefaultLayerFilter> DefaultLayerFilter { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionstring = new SqlConnectionStringBuilder()
            {
                DataSource = @"C:\Users\serjd\source\repos\NewLaserProject\NewLaserProject\Data\laserDatabase.db"
            }.ToString();

            optionsBuilder.UseSqlite(connectionstring);
        }
    }
}
