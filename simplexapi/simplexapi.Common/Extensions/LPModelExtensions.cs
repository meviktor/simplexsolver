using simplexapi.Common.Exceptions;
using simplexapi.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.Extensions
{
    public static class LPModelExtensions
    {
        /// <summary>
        /// This function runs the two-phase simplex algoritm on the given LP model.
        /// </summary>
        /// <param name="model">The LP model on which the algorithm will be executed.</param>
        /// <returns>The LP model in dictionary form with an optimal solution if any.</returns>
        /// <exception cref="SimplexAlgorithmExectionException">When the algorithm cannot return an optimal solution.</exception>
        public static LPModel TwoPhaseSimplex(this LPModel model)
        {
            model.AsStandard().AsDictionary();

            if (model.FirstPhaseNeeded())
            {
                model.BackToStandardFromDictionary()
                     .ToFirstPhaseDictionaryForm()
                     .RunSimplex();

                if (model.Objective.FunctionValue != 0)
                {
                    throw new SimplexAlgorithmExectionException(SimplexAlgorithmExectionErrorType.NoSolution);
                }

                model.ToSecondPhaseDictionaryForm();
            }

            return model.RunSimplex();
        }

        /// <summary>
        /// Turns the LP model into standard form, so the model contains only inequations with <= direction, uses variables having zero lower bound and the aim is maximizing the objective functions value.
        /// </summary>
        /// <param name="model">The LP model wanted to be transformed to standard form.</param>
        /// <returns>The LP model itself in standard form.</returns>
        private static LPModel AsStandard(this LPModel model)
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
                    var newVariable = new Variable { Name = variableAndRange.Value.LeftSide.Single().Variable.Value.Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                    var alias = new Equation
                    {
                        // e.g. x1 = x2 + 3 (<var> = <var> <const>)
                        LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = variableAndRange.Key } },
                        SideConnection = SideConnection.Equal,
                        RightSide = new Term[] { new Term { SignedCoefficient = 1, Variable = newVariable }, variableAndRange.Value.RightSide.Single() },
                    };

                    model.AllVariables.Add(newVariable);
                    // e.g. x2 >= 0 (<var> >= 0)
                    model.InterpretationRanges.Add(newVariable.GreaterOrEqualThenZeroRange());
                    model.StandardFormAliases.Add(alias);

                    // searching for the badly limited variable in the constraints and replacing them with the new one
                    foreach(var constraint in model.Constraints)
                    {
                        constraint.ReplaceVarWithExpression(variableAndRange.Key, alias.RightSide);
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
                        constraint.ReplaceVarWithExpression(variableAndRange.Key, alias.RightSide);
                    }
                }
            }
            #endregion

            #region transforming inequations with constraint >= to new ones having the constraint <=
            model.Constraints.Where(constraint => constraint.SideConnection == SideConnection.GreaterThanOrEqual).ForAll(constraint => constraint.Multiply(-1));
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
        private static LPModel AsDictionary(this LPModel model)
        {
            model.Constraints.ForAll(constraint =>
            {
                var newSlackVariable = new Variable { Name = model.AllVariables.First().Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                // this line transfers the terms of the left side to the right side
                constraint.Add(constraint.DeepCopy().LeftSide.Multiply(-1));
                constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = 1, Variable = newSlackVariable } });
                constraint.SideConnection = SideConnection.Equal;

                model.AllVariables.Add(newSlackVariable);
                model.InterpretationRanges.Add(newSlackVariable.GreaterOrEqualThenZeroRange());
            });

            return model;
        }

        /// <summary>
        /// This function transforms the LP model from dictionary from back to standard form.
        /// This opertion is needed e.g. when one or more of the basis variables has/have a negative value thus the first phase must be executed.
        /// </summary>
        /// <param name="model">The LP model which will be transformed back to standard form.</param>
        /// <returns>The LP model in standard form.</returns>
        private static LPModel BackToStandardFromDictionary(this LPModel model)
        {
            for (int i = 0; i < model.Constraints.Count; ++i)
            {
                var constraint = model.Constraints[i];

                var slackVariable = constraint.LeftSide.Single().Variable.Value;
                // the right side without the constant
                var originalLeftSide = constraint.RightSide.Where(term => term.Variable.HasValue).Multiply(-1).ToList();
                // the only constant term
                var originalRightSide = constraint.RightSide.Where(term => !term.Variable.HasValue).ToList();
                // left side <= right side
                var originalSideConnection = SideConnection.LessThanOrEqual;

                constraint = new Equation
                {
                    LeftSide = originalLeftSide,
                    RightSide = originalRightSide,
                    SideConnection = originalSideConnection
                };

                model.AllVariables.Remove(slackVariable);
                model.InterpretationRanges.Remove(model.InterpretationRanges.Where(range => range.LeftSide.Any(term => term.Variable?.Equals(slackVariable) ?? false)).Single());
            }
            return model;
        }

        /// <summary>
        /// This function transforms the LP model to such a dictionary form on which the first phase of the two-phase simplex algorithm can be ececuted.
        /// </summary>
        /// <param name="model">The LP model.</param>
        /// <returns>The transformed LP model.</returns>
        private static LPModel ToFirstPhaseDictionaryForm(this LPModel model)
        {
            #region Adding -var0 to the left side of the constarints & the slack variables
            var var0 = new Variable { Name = model.AllVariables.First().Name, Index = 0 };
            model.AllVariables.Add(var0);

            model.Constraints.ForAll(constraint =>
            {
                constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = -1, Variable = var0 } });

                var newSlackVariable = new Variable { Name = model.AllVariables.First().Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                model.AllVariables.Add(newSlackVariable);

                constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = 1, Variable = newSlackVariable } });
                constraint.SideConnection = SideConnection.Equal;
            });
            #endregion

            #region Expressing the var0 variable from the constraint which has the most negative right side
            var mostNegativeRightSidedConstraint = model.Constraints.OrderBy(constraint => constraint.RightSide.Single(term => !term.Variable.HasValue).SignedCoefficient).First();
            // on the right side there must be only one single constant (and nothing else) anyway so the predicate in the First() call is not necessary
            var rightSideConstant = mostNegativeRightSidedConstraint.RightSide.Single(term => !term.Variable.HasValue);

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
                    var constantOnRight = constraint.RightSide.Single(term => !term.Variable.HasValue);

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

            return model;
        }
        
        /// <summary>
        /// If there was a first phase and the optimum of the objective function was 0 the model should be transformed to such a dictionary form on which the second phase can be executed.
        /// This function does the necessary changes on the model.
        /// </summary>
        /// <param name="model">The LP model wanted to be transformed to be eligible to the execution of the second phase.</param>
        /// <returns>The transformed LP model.</returns>
        private static LPModel ToSecondPhaseDictionaryForm(this LPModel model)
        {
            #region Throw out the 'var0 = 0' shaped constraint if any
            var constraintToRemove = model.Constraints.Where(constraint => constraint.LeftSide.Count == 1 && constraint.LeftSide.Any(term => term.Variable.Value.Index == 0) &&
                                                                           (constraint.RightSide.Count == 1 || constraint.RightSide.Any(term => !term.Variable.HasValue && term.SignedCoefficient == 0)) || constraint.RightSide.Count == 0)
                                                      .SingleOrDefault();
            if(constraintToRemove != null)
            {
                model.Constraints.Remove(constraintToRemove);
            }
            #endregion

            #region If var0 is a basis variable it will be exchanged with a non-basis variable by a pivot step
            var constraintWithVar0Basis = model.Constraints.Where(constraint => constraint.LeftSide.Count == 1 && constraint.LeftSide.Any(term => term.Variable.Value.Index == 0))
                                                           .SingleOrDefault();
            // Pivot step - TODO: organize this functionality to a seperate function
            if (constraintToRemove != null)
            {
                var negatedVar0Term = new Term { SignedCoefficient = -1, Variable = new Variable { Name = model.AllVariables.First().Name, Index = 0 } };
                // choosing the variable has a negative coefficient and the smallest index
                var negatedNewBasisTerm = constraintWithVar0Basis.RightSide.Where(term => term.Variable.Value.Index == constraintWithVar0Basis.RightSide.Where(t => t.Variable.HasValue && t.SignedCoefficient < 0).Min(t => t.Variable.Value.Index)).Single();
                constraintWithVar0Basis.Add(new Term[] { negatedVar0Term, negatedNewBasisTerm });
                constraintWithVar0Basis.Multiply(1 / constraintWithVar0Basis.LeftSide.First(term => term.Variable.Value.Index == negatedNewBasisTerm.Variable.Value.Index).SignedCoefficient);

                model.Constraints.Where(constraint => constraint != constraintWithVar0Basis)
                    .ForAll(constraint => constraint.ReplaceVarWithExpression(negatedNewBasisTerm.Variable.Value, constraintWithVar0Basis.RightSide));
            }
            #endregion

            #region Throw out the rest of the var0 occurences
            model.Constraints.ForAll(constraint =>
            {
                var foundVar0 = constraint.RightSide.Where(term => term.Variable?.Equals(new Variable { Name = model.AllVariables.First().Name, Index = 0 }) ?? false).SingleOrDefault();
                if (foundVar0 != null)
                {
                    constraint.RightSide.Remove(foundVar0);
                }
            });
            #endregion

            #region Set back the original objective function exhanging the basis variables with their equivaltent expressions from the dictionary (right sides)
            model.Objective = model.TmpObjective;
            model.Constraints.ForAll(constraint =>
            {
                var basisVariable = constraint.LeftSide.Single(term => term.Variable.HasValue).Variable.Value;
                model.Objective.Function.ReplaceVarWithExpression(basisVariable, constraint.RightSide);
            });
            #endregion

            return model;
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
                model.Objective.Function.LeftSide.Single().SignedCoefficient *= -1;
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
                        .Where(range => (range.LeftSide.Single().Variable.Value.Equals(variable)) && // the variable can be found
                                        (!range.RightSide.Single().Variable.HasValue && range.RightSide.Single().SignedCoefficient != 0)) // the limit is a non-zero constant
                        .First();
                    result.Add(variable, interpretationRangeOfVariable);
                }
            }

            return result;
        }

        /// <summary>
        /// Decides wheter a first phase is needed or not on the current dictionary form LP model.
        /// </summary>
        /// <param name="model">The LP model in dictionary form.</param>
        /// <returns>Wheter a first phase is needed or not.</returns>
        private static bool FirstPhaseNeeded(this LPModel model)
        {
            return model.Constraints.Any(constraint => constraint.RightSide.Any(term => !term.Variable.HasValue && term.SignedCoefficient < 0));
        }

        /// <summary>
        /// Runs the simplex algoritm on the given LP model.
        /// </summary>
        /// <param name="model">The LP model on which the simplex algoritm will be executed.</param>
        /// <returns></returns>
        private static LPModel RunSimplex(this LPModel model)
        {
            return model;
        }
    }
}
