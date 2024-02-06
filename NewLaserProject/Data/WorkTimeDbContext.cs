using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NewLaserProject.Data.Models;

namespace NewLaserProject.Data
{
    public class  WorkTimeDbContext:DbContext
    {
        public WorkTimeDbContext(DbContextOptions<WorkTimeDbContext> options):base(options) { }
        public WorkTimeDbContext() { }
        public DbSet<WorkTimeLog> WorkTimeLogs { get; set; }
        public DbSet<ProcTimeLog> ProcTimeLogs {  get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    var str = new SqliteConnectionStringBuilder(@"data source=Database\worktimeDatabase.db")
        //        .ToString();
        //    optionsBuilder.UseSqlite(@"data source=Database\worktimeDatabase.db");
        //}
    }
}
