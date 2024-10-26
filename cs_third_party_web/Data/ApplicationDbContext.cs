using cs_third_party_web.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using static cs_third_party_web.Models.CsDb;
using static cs_third_party_web.Models.LocalDb;

namespace cs_third_party_web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        { }

        public DbSet<Project> Projects { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<MapCsHrmEmployee> MapCsHrmEmployees { get; set; }
        public DbSet<ProjectType> ProjectTypes { get; set; }
        public DbSet<AttendanceLogRequest> AttendanceLogRequests { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            base.OnModelCreating(builder);
        }
    }

    ////HRM DB
    //public class HrmDbContext : DbContext
    //{
    //    public HrmDbContext(DbContextOptions<HrmDbContext> options) : base(options)
    //    { }

    //    //

    //    protected override void OnModelCreating(ModelBuilder builder)
    //    {
    //        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
    //        {
    //            relationship.DeleteBehavior = DeleteBehavior.Restrict;
    //        }

    //        base.OnModelCreating(builder);
    //    }
    //}

    ////CS DB
    //public class CsDbContext : DbContext
    //{
    //    public CsDbContext(DbContextOptions<CsDbContext> options) : base(options)
    //    { }

    //    public DbSet<CsAttendanceLog> CsAttendanceLogs { get; set; }
    //    public DbSet<CsEmployee> CsEmployees { get; set; }
    //    protected override void OnModelCreating(ModelBuilder builder)
    //    {
    //        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
    //        {
    //            relationship.DeleteBehavior = DeleteBehavior.Restrict;
    //        }

    //        base.OnModelCreating(builder);
    //    }
    //}
}
