using Microsoft.AspNetCore.Mvc;
using simplexapi.Common.Exceptions;
using simplexapi.Common.Extensions;
using simplexapi.Common.Models;
using simplexapi.Constants;
using System;

namespace simplexapi.Controllers
{
    [Route("[controller]/[action]")]
    public class SimplexSolverController : Controller
    {
        public IActionResult Ping()
        {
            return Json(new { status = "OK" });
        }

        [HttpPost]
        public IActionResult Solve([FromBody] LPModelDto lpModelDto)
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
            return Json(new { success = solution != null, message = message, solution = solution });
        }
    }
}
