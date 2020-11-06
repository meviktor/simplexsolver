using simplexapi.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.Extensions
{
    public static class LPModelExtensions
    {
        /// <summary>
        /// Turns the LP model into standard form, so the model contains only inequations with <= direction, uses variables having zero lower bound and the aim is maximizing the objective functions value.
        /// </summary>
        /// <param name="model">The LP model wanted to be transformed to standard form.</param>
        /// <returns>The LP model itself in standard form.</returns>
        public static LPModel AsStandard(this LPModel model)
        {
            #region transforming equations to inequations
            var eqConstraints = model.Constraints.Where(constraint => constraint.SideConnection == SideConnection.Equal);
            foreach (var constraint in eqConstraints)
            {
                model.Constraints.Add(
                    new Equation
                    {
                        LeftSide = constraint.LeftSide.ToList(),
                        SideConnection = SideConnection.LessThanOrEqual,
                        RightSide = constraint.RightSide.ToList()
                    }
                );
                model.Constraints.Add(
                    new Equation
                    {
                        LeftSide = constraint.LeftSide.ToList(),
                        SideConnection = SideConnection.GreaterThanOrEqual,
                        RightSide = constraint.RightSide.ToList()
                    }
                );
                model.Constraints.Remove(constraint);
            }
            #endregion

            #region changing limitless variables or variables with non-zero lower bound to new ones having zero lower bound
            var varsWithNonZeroOrWithoutLimit = model.FindVariablesWithNoLimitOrNonZeroLimit();
            foreach (var variableAndRange in varsWithNonZeroOrWithoutLimit)
            {
                if(variableAndRange.Value != null)
                {
                    var newVariable = new Variable { Name = variableAndRange.Value.LeftSide.First().Variable.Value.Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                    var alias = new Equation
                    {
                        // e.g. x1 = x2 + 3 (<var> = <var> <const>)
                        LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = variableAndRange.Key } },
                        SideConnection = SideConnection.Equal,
                        RightSide = new Term[] { new Term { SignedCoefficient = 1, Variable = newVariable }, variableAndRange.Value.RightSide.First() },
                    };

                    model.AllVariables.Add(newVariable);
                    // e.g. x2 >= 0 (<var> >= 0)
                    model.InterpretationRanges.Add(newVariable.GreaterOrEqualThenZeroRange());
                    model.StandardFormAliases.Add(alias);

                    // searching for the badly limited variable in the constraints and replacing them with the new one
                    foreach(var constraint in model.Constraints)
                    {
                        var foundOccurecne = constraint.LeftSide.FirstOrDefault(term => term.Variable?.Equals(variableAndRange.Key) ?? false);
                        if(foundOccurecne != null)
                        {
                            // multiply the alias with the coefficient of the original decision variable
                            var multipliedAlias = new Term[] { new Term { SignedCoefficient = foundOccurecne.SignedCoefficient, Variable = newVariable }, new Term { SignedCoefficient = variableAndRange.Value.RightSide.First().SignedCoefficient * foundOccurecne.SignedCoefficient } };

                            var transorm = multipliedAlias.ToList();
                            transorm.Add(new Term { SignedCoefficient = foundOccurecne.SignedCoefficient * -1, Variable = foundOccurecne.Variable });

                            constraint.Add(transorm);
                        }
                    }
                }
                else
                {
                    var newVariable1 = new Variable { Name = variableAndRange.Value.LeftSide.First().Variable.Value.Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                    var newVariable2 = new Variable { Name = variableAndRange.Value.LeftSide.First().Variable.Value.Name, Index = model.AllVariables.Max(var => var.Index) + 2 };
                    var alias = new Equation
                    {
                        // e.g. x1 = x2 - x3 (<limitless_var> = <zero_limit_var1> - <zero_limit_var2>
                        LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = variableAndRange.Key } },
                        SideConnection = SideConnection.Equal,
                        RightSide = new Term[] { new Term { SignedCoefficient = 1, Variable = newVariable1 }, new Term { SignedCoefficient = -1, Variable = newVariable2 } }
                    };

                    model.AllVariables.Add(newVariable1);
                    model.AllVariables.Add(newVariable2);
                    model.InterpretationRanges.Add(newVariable1.GreaterOrEqualThenZeroRange());
                    model.InterpretationRanges.Add(newVariable2.GreaterOrEqualThenZeroRange());
                    model.StandardFormAliases.Add(alias);

                    // searching for the limitless variable in the constraints and replacing them with the new expression
                    foreach (var constraint in model.Constraints)
                    {
                        var foundOccurecne = constraint.LeftSide.FirstOrDefault(term => term.Variable?.Equals(variableAndRange.Key) ?? false);
                        if (foundOccurecne != null)
                        {
                            // multiply the alias with the coefficient of the original decision variable
                            var multipliedAlias = new Term[] { new Term { SignedCoefficient = foundOccurecne.SignedCoefficient, Variable = newVariable1 }, new Term { SignedCoefficient = foundOccurecne.SignedCoefficient * -1, Variable = newVariable2 } };

                            var transorm = multipliedAlias.ToList();
                            transorm.Add(new Term { SignedCoefficient = foundOccurecne.SignedCoefficient * -1, Variable = foundOccurecne.Variable });

                            constraint.Add(transorm);
                        }
                    }
                }
            }
            #endregion

            #region transforming inequations with constraint >= to new ones having the constraint <=
            foreach (var constraint in model.Constraints)
            {
                if(constraint.SideConnection == SideConnection.GreaterThanOrEqual)
                {
                    constraint.Multiply(-1);
                }
            }
            #endregion

            #region changing the optimization aim to max if it was min
            model.ChangeOptimizationAimToMaximize();
            #endregion

            return model;
        }

        /// <summary>
        /// This function transfers the LP model to dictionary form - the left sides of the constraints will contain the basic variables, the right side the "rest" of the constraint.
        /// The constants on the right side are the values of the basic variables. The constant in the objective function is the value wanted to be maximized.
        /// </summary>
        /// <param name="model">The LP model wanted to be transformed to dictionary form.</param>
        /// <returns>The LP model itself in dictionary format.</returns>
        public static LPModel AsDictionary(this LPModel model)
        {
            foreach(var constraint in model.Constraints)
            {
                var newSlackVariable = new Variable { Name = model.AllVariables.First().Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                // this line transfers the terms of the left side to the right side
                constraint.Add(constraint.DeepCopy().Multiply(-1).LeftSide);
                constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = 1, Variable = newSlackVariable } });
                constraint.SideConnection = SideConnection.Equal;

                model.AllVariables.Add(newSlackVariable);
                model.InterpretationRanges.Add(newSlackVariable.GreaterOrEqualThenZeroRange());
            }

            return model;
        }

        /// <summary>
        /// This function determines if we need to execute the first phase of the two-phase simplex algorithm or not.
        /// If so, as a side effect the function transforms the LP model to a dictionary form on which the first phase can be executed.
        /// If not, the function does not modify the LP model. In this case we have to use <see cref="AsDictionary"/> extension method to transform the LP model to a dictionary form on which the second phase has to be executed.
        /// </summary>
        /// <param name="model">The LP model.</param>
        /// <returns>A first phase is needed or not.</returns>
        public static bool TransformToHelperForm(this LPModel model)
        {
            bool isDictionaryInValid = model.Constraints.Any(constraint => constraint.RightSide.Any(term => !term.Variable.HasValue && term.SignedCoefficient < 0));
            if (isDictionaryInValid)
            {
                #region Adding -var0 to the left side of the constarints & the slack variables
                var var0 = new Variable { Name = model.AllVariables.First().Name, Index = 0 };
                model.AllVariables.Add(var0);

                foreach (var constraint in model.Constraints)
                {
                    constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = -1, Variable = var0 } });

                    var newSlackVariable = new Variable { Name = model.AllVariables.First().Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                    model.AllVariables.Add(newSlackVariable);

                    constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = 1, Variable = newSlackVariable } });
                    constraint.SideConnection = SideConnection.Equal;
                }
                #endregion

                #region Expressing the var0 variable from the constraint which has the most negative right side
                var mostNegativeRightSidedConstraint = model.Constraints.OrderBy(constraint => constraint.RightSide.First(term => !term.Variable.HasValue).SignedCoefficient).First();
                // on the right side there must be only one single constant (and nothing else) anyway so the predicate in the First() call is not necessary
                var rightSideConstant = mostNegativeRightSidedConstraint.RightSide.First(term => !term.Variable.HasValue);

                mostNegativeRightSidedConstraint.Add(new Term[] { new Term { SignedCoefficient = 1, Variable = var0 } });
                mostNegativeRightSidedConstraint.Add(new Term[] { new Term { SignedCoefficient = rightSideConstant.SignedCoefficient * -1, Variable = rightSideConstant.Variable } });
                mostNegativeRightSidedConstraint.ChangeSides();
                #endregion

                #region Expressing the slack variables from the other constraints and replacing var0-s with its equivalent expression got in the previous step
                foreach (var constraint in model.Constraints)
                {
                    if (!constraint.Equals(mostNegativeRightSidedConstraint))
                    {
                        // we have added the slack variables to the constraints after the var0 and the decision variables - so the slack variable must have the highest index in the constraint
                        var slackVariableTerm = constraint.LeftSide.OrderByDescending(term => term.Variable.Value.Index).First();
                        var constantOnRight = constraint.RightSide.First(term => !term.Variable.HasValue);

                        constraint.Add(new Term[] { new Term { SignedCoefficient = slackVariableTerm.SignedCoefficient * -1, Variable = slackVariableTerm.Variable } });
                        constraint.Add(new Term[] { new Term { SignedCoefficient = constantOnRight.SignedCoefficient * -1 } });
                        constraint.ChangeSides();

                        // mostNegativeRightSidedConstraint shape is something like this: var0 = <expression on the right>
                        constraint.ReplaceVarWithExpression(var0, mostNegativeRightSidedConstraint.RightSide);
                    }
                }
                #endregion

                #region Changing the objective function to -var0 (to its equivalent expression) and the optimization aim to maximize

                #region Saving the old objective function to be able to write back after the fist phase of the simplex algorithm is done
                model.TmpObjective = model.Objective;
                #endregion

                model.Objective = new Objective
                (
                    OptimizationAim.Maximize,
                    new Equation
                    {
                        LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = new Variable { Name = "w", Index = 0 } } },
                        SideConnection = SideConnection.Equal,
                        RightSide = mostNegativeRightSidedConstraint.DeepCopy().RightSide.Multiply(-1) as IList<Term>
                    }
                );
                #endregion

                return true;
            }
            return false;
        }

        /// <summary>
        /// Changes the LP models optimization aim to maximize.
        /// </summary>
        /// <param name="model">The LP model whose optimization aim will be changed.</param>
        /// <returns>The LP model having a maximizing optimization aim.</returns>
        private static LPModel ChangeOptimizationAimToMaximize(this LPModel model)
        {
            if (model.Objective.Aim == OptimizationAim.Minimize)
            {
                model.Objective.Aim = OptimizationAim.Maximize;
                model.Objective.Function.Multiply(-1);
                // multiplying both sides of the function adds a minus sign (-) prefix to to funtion name - we don't want to leave this
                model.Objective.Function.LeftSide.First().SignedCoefficient *= -1;
            }
            return model;
        }

        /// <summary>
        /// Searhes for variables having non-zero limit or having no limit at all.
        /// </summary>
        /// <param name="model">The LP model in which the search is executed.</param>
        /// <returns>A dictionary contains the variables as key and their interpretation range as value.</returns>
        private static Dictionary<Variable, Equation> FindVariablesWithNoLimitOrNonZeroLimit(this LPModel model)
        {
            var result = new Dictionary<Variable, Equation>();

            foreach(var variable in model.DecisionVariables)
            {
                // the variable has no limit - cannot be found
                var withNoLimit = !model.InterpretationRanges.Any(
                    range => range.LeftSide.Any(term => term.Variable.Value.Name == variable.Name && term.Variable.Value.Index == variable.Index)
                );
                if (withNoLimit)
                {
                    result.Add(variable, null);
                }
                else
                {
                    // variable with non-zero lower limit
                    var interpretationRangeOfVariable = model.InterpretationRanges
                        .Where(range => (range.LeftSide.First().Variable.Value.Name == variable.Name && range.LeftSide.First().Variable.Value.Index == variable.Index) && // the variable can be found
                                        (!range.RightSide.First().Variable.HasValue && range.RightSide.First().SignedCoefficient != 0)) // the limit is a non-zero constant
                        .First();
                    result.Add(variable, interpretationRangeOfVariable);
                }
            }

            return result;
        }
    }
}
