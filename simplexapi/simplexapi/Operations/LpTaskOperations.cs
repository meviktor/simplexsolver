using Microsoft.EntityFrameworkCore;
using simplexapi.Data;
using simplexapi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace simplexapi.Operations
{
    public class LpTaskOperations
    {
        private readonly SimplexSolverDbContext _ctx;

        public LpTaskOperations(SimplexSolverDbContext simplexSolverContext)
        {
            _ctx = simplexSolverContext;
        }
        public async Task Add(LpTask lpTask)
        {
            _ctx.LpTasks.Add(lpTask);
            await _ctx.SaveChangesAsync();
        }

        public async Task Remove(LpTask lpTask)
        {
            _ctx.LpTasks.Remove(lpTask);
            await _ctx.SaveChangesAsync();
        }

        public async Task<LpTask> FindById(Guid lpTaskId) => await _ctx.LpTasks.SingleOrDefaultAsync(lpTask => lpTask.Id == lpTaskId);

        public async Task<IEnumerable<LpTask>> Page(int from, int to) => await _ctx.LpTasks.OrderByDescending(lpTask => lpTask.SolvedAt).Skip(from).Take(to).ToListAsync();

        public async Task<IEnumerable<LpTask>> GetTheLast(int itemCount) => await Page(0, itemCount);
    }
}
