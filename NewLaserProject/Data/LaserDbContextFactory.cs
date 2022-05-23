using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NewLaserProject.Data
{
    public class LaserDbContextFactory : IDesignTimeDbContextFactory<LaserDbContext>
    {
        public LaserDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LaserDbContext>();
            optionsBuilder.UseSqlite("Data Source=laserTechnologies.db");
            return new LaserDbContext(optionsBuilder.Options);
        }
    }
}
