using Microsoft.EntityFrameworkCore;
using simplexapi.Models;

namespace simplexapi.Data
{
    public class SimplexSolverDbContext : DbContext
    {
        public SimplexSolverDbContext(DbContextOptions<SimplexSolverDbContext> options) : base(options)
        {
        }

        public DbSet<LpTask> LpTasks { get; set; }
        public DbSet<LpIterationLog> LpIterationLogs { get; set; }
    }
}
