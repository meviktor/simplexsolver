﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using simplexapi.Common.Exceptions;
using simplexapi.Common.Extensions;
using simplexapi.Common.IP;
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
        public IActionResult DualSimplex([FromBody] LPModelDto lpModelDto)
        {
            bool wrongFormat = false;
            string message = null;

            try
            {
                lpModelDto.Validate();
            }
            catch (ArgumentException e)
            {
                wrongFormat = true;
                message = string.Format(Messages.WRONG_FORMAT_CHECK_ARG, e.ParamName);
            }

            if (wrongFormat)
            {
                return BadRequest(new { success = false, message = message });
            }

            var lpModel = lpModelDto.MapTo(new LPModel());

            try
            {
                lpModel.DualSimplex();
                var solution = lpModel.GetSolutionFromDictionary(lpModelDto.MapTo(new LPModel()).Objective.Function);
            }
            catch (SimplexAlgorithmExectionException e)
            {
                message = e.ExecutionError == SimplexAlgorithmExectionErrorType.NoSolution ?
                       Messages.SIMPLEX_RESULT_NO_SOLUTION :
                       Messages.SIMPLEX_RESULT_NO_LIMIT;
            }

            return Json(new { success = true, message = message});
        }

        [HttpPost]
        public async Task<IActionResult> Solve([FromBody] LPModelDto lpModelDto, bool integerProgramming = false)
        {
            bool wrongFormat = false;
            string message = null;
            SimplexSolutionDto solution = null;
            LPModel lpModel = null;

            try
            {
                lpModelDto.Validate();
            }
            catch (ArgumentException e)
            {
                wrongFormat = true;
                message = string.Format(Messages.WRONG_FORMAT_CHECK_ARG, e.ParamName);
            }

            if (wrongFormat)
            {
                return BadRequest(new { success = false, message = message });
            }

            if (integerProgramming)
            {
                try
                {
                    lpModel = Gomory.RunGomory(lpModelDto);
                    solution = lpModel.GetSolutionFromDictionary(lpModelDto.MapTo(new LPModel()).Objective.Function);
                }
                catch (SimplexAlgorithmExectionException e)
                {
                    message = e.ExecutionError == SimplexAlgorithmExectionErrorType.NoSolution ?
                           Messages.SIMPLEX_INT_RESULT_NO_SOLUTION :
                           Messages.SIMPLEX_INT_RESULT_NO_LIMIT;
                }
            }
            else
            {
                lpModel = lpModelDto.MapTo(new LPModel());

                try
                {
                    lpModel.TwoPhaseSimplex();
                    solution = lpModel.GetSolutionFromDictionary(lpModelDto.MapTo(new LPModel()).Objective.Function);
                }
                catch (SimplexAlgorithmExectionException e)
                {
                    message = e.ExecutionError == SimplexAlgorithmExectionErrorType.NoSolution ?
                           Messages.SIMPLEX_RESULT_NO_SOLUTION :
                           Messages.SIMPLEX_RESULT_NO_LIMIT;
                }
                catch(Exception)
                {
                    message = Messages.GENERAL_ERROR;
                }
            }

            if(message == Messages.GENERAL_ERROR)
            {
                return StatusCode(500, new { success = false, message = message });
            }

            var lpTask = new LpTask
            {
                LPModelAsJson = JsonConvert.SerializeObject(lpModelDto),
                SolutionAsJson = JsonConvert.SerializeObject(new LPTaskResultDto{ SolutionFound = solution != null, Message = message, Solution = solution }),
                SolvedAt = DateTimeOffset.Now,
                IntegerProgramming = integerProgramming
            };

            await _lpTaskOperations.Add(lpTask);

            return Json(new { success = true, taskId = lpTask.Id });
        }

        public async Task<IActionResult> TaskResult(Guid taskId)
        {
            var foundTask = await _lpTaskOperations.FindById(taskId);
            if(foundTask == null)
            {
                return BadRequest(new { message = $"There is no solved task with id {taskId}" });
            }
            return Json(new
            {
                solvedAt = foundTask.SolvedAt,
                integerProgramming = foundTask.IntegerProgramming,
                lpModel = JsonConvert.DeserializeObject<LPModelDto>(foundTask.LPModelAsJson),
                solution = JsonConvert.DeserializeObject<LPTaskResultDto>(foundTask.SolutionAsJson)
            });
        }

        public async Task<IActionResult> HistoryItems(int itemCount)
        {
            if(itemCount <= 0)
            {
                return BadRequest(new { message = "Count of history items must be a positive number!" });
            }

            var historyItems = await _lpTaskOperations.GetHistoryItems(itemCount);
            return Json(historyItems);
        }

        public async Task<IActionResult> HistoryItemCount()
        {
            return Json(new { itemCount = await _lpTaskOperations.ItemsTotal() });
        }
    }
}
