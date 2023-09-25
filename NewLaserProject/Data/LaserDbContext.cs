using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewLaserProject.Data.Models;

namespace NewLaserProject.Data
{
    public class LaserDbContext : DbContext
    {
        public LaserDbContext(DbContextOptions<LaserDbContext> options) : base(options)
        {
        }
        public LaserDbContext()
        {
        }
        public DbSet<Material> Material
        {
            get; set;
        }
        public DbSet<Technology> Technology
        {
            get; set;
        }
        public DbSet<MaterialEntRule> MaterialEntRule
        {
            get; set;
        }
        public DbSet<DefaultLayerEntityTechnology> DefaultLayerEntityTechnology
        {
            get; set;
        }
        public DbSet<DefaultLayerFilter> DefaultLayerFilter
        {
            get; set;
        }

        public async Task LoadSetsAsync()
        {
            await Task.WhenAll(
                    Material.LoadAsync(),
                    Technology.LoadAsync(),
                    MaterialEntRule.LoadAsync(),
                    DefaultLayerEntityTechnology.LoadAsync(),
                    DefaultLayerFilter.LoadAsync()
                );
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    var connectionstring = new SqlConnectionStringBuilder()
        //    {
        //        DataSource = Path.Join( ProjectPath.GetFolderPath("Data"), "laserDatabase.db")
        //    }.ToString();

        //    optionsBuilder.UseSqlite(connectionstring);
        //}
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //    modelBuilder.Entity<Material>()
        //        .HasOne<MaterialEntRule>()
        //        .WithOne(m => m.Material)
        //        .HasForeignKey<MaterialEntRule>(mer => mer.MaterialId);
        //}
    }
}
