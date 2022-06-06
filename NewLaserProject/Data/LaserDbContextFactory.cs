using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Data.SqlClient;
using System.IO;

namespace NewLaserProject.Data
{
    //public class LaserDbContextFactory : IDesignTimeDbContextFactory<LaserDbContext>
    //{
    //    public LaserDbContext CreateDbContext(string[] args)
    //    {
    //        var optionsBuilder = new DbContextOptionsBuilder<LaserDbContext>();
    //        var connectionString = new SqlConnectionStringBuilder()
    //        {
    //            DataSource = "laserTechnologies.db"
    //        }
    //        .ToString();

    //        var filepath = Path.Join(ProjectPath.GetFolderPath("Data"), "laserDatabase.db");
    //        optionsBuilder.UseSqlite($"Data Source = {filepath}");
    //        return new LaserDbContext(optionsBuilder.Options);
    //    }
    //}
}
