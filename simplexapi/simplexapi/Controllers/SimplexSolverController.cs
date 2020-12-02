using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using simplexapi.Common.Exceptions;
using simplexapi.Common.Extensions;
using simplexapi.Common.Models;
using simplexapi.Constants;
using simplexapi.Models;
using simplexapi.Operations;
using System;
using System.Threading.Tasks;

namespace simplexapi.Controllers
{
    [Route("[controller]/[action]")]
    public class SimplexSolverController : Controller
    {
        private readonly LpTaskOperations _lpTaskOperations;
        public SimplexSolverController(LpTaskOperations lpTaskOperations)
        {
            _lpTaskOperations = lpTaskOperations;
        }
        public IActionResult Ping()
        {
            return Json(new { status = "OK" });
        }

        [HttpPost]
        public async Task<IActionResult> Solve([FromBody] LPModelDto lpModelDto)
        {
            bool wrongFormat = false;
            string message = null;
            SimplexSolutionDto solution = null;

            try
            {
                lpModelDto.Validate();
            }
            catch(ArgumentException e)
            {
                wrongFormat = true;
                message = string.Format(Messages.WRONG_FORMAT_CHECK_ARG, e.ParamName);
            }

            var lpModel = lpModelDto.MapTo(new LPModel());

            try
            {
                lpModel.TwoPhaseSimplex();
                solution = lpModel.GetSolutionFromDictionary();
            }
            catch (SimplexAlgorithmExectionException e)
            {
                message = e.ExecutionError == SimplexAlgorithmExectionErrorType.NoSolution ?
                       Messages.SIMPLEX_RESULT_NO_SOLUTION :
                       Messages.SIMPLEX_RESULT_NO_LIMIT;
            }

            if (wrongFormat)
            {
                return BadRequest(new { success = false, message = message });
            }

            var lpTask = new LpTask
            {
                LPModelAsJson = JsonConvert.SerializeObject(lpModelDto),
                SolutionAsJson = JsonConvert.SerializeObject(new { solutionFound = solution != null, message = message, solution = solution }),
                SolvedAt = DateTimeOffset.Now
            };

             await _lpTaskOperations.Add(lpTask);

            return Json(new { success = true, taskId = lpTask.Id });
        }
    }
}
