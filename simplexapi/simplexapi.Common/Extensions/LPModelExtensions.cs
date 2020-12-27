using simplexapi.Common.Exceptions;
using simplexapi.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.Extensions
{
    /// <summary>
    /// Constains extension methods for running the two-phase simplex algoritm on a LP model.
    /// </summary>
    public static class LPModelExtensions
    {
        /// <summary>
        /// Runs the dual simplex algoritm on the given LP model.
        /// </summary>
        /// <param name="lpModel">The LP model on wich the dual simplex method will be executed.</param>
        /// <returns>The LP model</returns>
        public static LPModel DualSimplex(this LPModel model)
        {
            while (!model.AllBasisVariableHaveNonNegativeValuesInTheDictionary())
            {
                #region Choosing variable will step out from the base - choosing the dictionary row having the most negative index - we will choose the lowest index anyway
                var stepOutVariable = model.Constraints.Where(dictionaryRow => (dictionaryRow.RightSide.SingleOrDefault(term => term.Constant)?.SignedCoefficient ?? 0) < 0)
                                                       .Select(dictionaryRow => new { Variable = dictionaryRow.LeftSide.Single().Variable.Value, Value = dictionaryRow.RightSide.Single(term => term.Constant).SignedCoefficient })
                                                       .OrderBy(variableValuePair => variableValuePair.Value)
                                                       .ThenBy(variableValuePair => variableValuePair.Variable.Index)
                                                       .First().Variable;
                #endregion

                #region Choosing the variable will step in the basis - by finding the smallest quotient
                var rowWithStepOutVariable = model.Constraints.Single(row => row.LeftSide.Single().Variable.Value.Equals(stepOutVariable));
                var termsWithPositiveCoefficient = rowWithStepOutVariable.RightSide.Where(term => term.SignedCoefficient > 0 && !term.Constant);
                var varsWithQuotient = new Dictionary<Variable, Rational>();

                termsWithPositiveCoefficient.ForAll(term =>
                {
                    var functionTermsCoefficient = model.Objective.Function.RightSide.SingleOrDefault(functionTerm => !functionTerm.Constant && functionTerm.Variable.Value.Equals(term.Variable.Value))?.SignedCoefficient ?? 0;
                    varsWithQuotient.Add(term.Variable.Value, functionTermsCoefficient / term.SignedCoefficient);
                });
                var stepInVariable = varsWithQuotient.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key.Index).First().Key;
                #endregion

                #region Making a pivot step
                model.MakePivotStep(stepInVariable, stepOutVariable);
                #endregion
            }

            return model;
        }

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
        /// Reads the solution from the dictionary.
        /// </summary>
        /// <param name="model">The LP model.</param>
        /// <returns>A <see cref="SimplexSolutionDto"></see> containing the solution. </returns>
        public static SimplexSolutionDto GetSolutionFromDictionary(this LPModel model, Equation objectiveFunction)
        {
            var decisionVariableValues = new List<VariableValuePairDto>();

            model.DecisionVariables.ForAll(decisionVariable =>
            {
                Func<Equation, Variable, bool> leftSideVariable = (equation, variable) => equation.LeftSide.Single().Variable?.Equals(variable) ?? false;
                bool isBasisVariable = model.Constraints.Any(constraint => leftSideVariable(constraint, decisionVariable));

                // value of a basis variable: the constant of its equation/constraint
                if (isBasisVariable)
                {
                    var value = model.Constraints.Single(constraint => leftSideVariable(constraint, decisionVariable)).RightSide.SingleOrDefault(term => term.Constant)?.SignedCoefficient ?? 0;
                    decisionVariableValues.Add( new VariableValuePairDto
                    {
                       Variable = new VariableDto { Index = decisionVariable.Index, Name = decisionVariable.Name },
                       Value = new RationalDto { Numerator = value.Numerator, Denominator = value.Denominator }
                    });
                }
                else
                {
                    bool isExpressedByOtherVariables = model.StandardFormAliases?.Any(aliasExpr => leftSideVariable(aliasExpr, decisionVariable)) ?? false;
                    // does not appear in the dictionary - was exchanged with an expression with new variables when the model was transformed into standard form in the beginning
                    if (isExpressedByOtherVariables)
                    {
                        var variableAlias = model.StandardFormAliases.Single(aliasExpr => leftSideVariable(aliasExpr, decisionVariable));
                        var dependentVariables = variableAlias.RightSide.Where(term => term.Variable.HasValue).Select(term => term.Variable.Value);
                        var dependentVariablesAndValues = new Dictionary<Variable, Rational>();

                        dependentVariables.ForAll(dependentVariable =>
                        {
                            bool isDependentVariableInBasis = model.Constraints.Any(constraint => leftSideVariable(constraint, dependentVariable));
                            if (isDependentVariableInBasis)
                            {
                                var dependentVarValue = model.Constraints.Single(constraint => leftSideVariable(constraint, dependentVariable)).RightSide.Single(term => !term.Variable.HasValue).SignedCoefficient;
                                dependentVariablesAndValues.Add(dependentVariable, dependentVarValue);
                            }
                            else
                            {
                                dependentVariablesAndValues.Add(dependentVariable, Rational.Zero);
                            }
                        });

                        // replace the variables in the alias expression with their value so we can get a numeric value for the original decision variable
                        Rational valueOfVariableAlias = Rational.Zero;
                        variableAlias.RightSide.ForAll(term =>
                        {
                            var termValue = term.Variable.HasValue ?
                                term.SignedCoefficient * dependentVariablesAndValues.Single(kv => kv.Key.Equals(term.Variable.Value)).Value :
                                term.SignedCoefficient;
                            valueOfVariableAlias += termValue;
                        });

                        decisionVariableValues.Add(new VariableValuePairDto
                        {
                           Variable = new VariableDto { Index = decisionVariable.Index, Name = decisionVariable.Name },
                           Value = new RationalDto { Numerator = valueOfVariableAlias.Numerator, Denominator = valueOfVariableAlias.Denominator }
                        });
                    }
                    // non-basis variable
                    else
                    {
                        decisionVariableValues.Add(new VariableValuePairDto
                        {
                            Variable = new VariableDto { Index = decisionVariable.Index, Name = decisionVariable.Name },
                            Value = new RationalDto { Numerator = Rational.Zero.Numerator, Denominator = Rational.Zero.Denominator }
                        });
                    }
                }
            });

            Rational objFuncVal = 0;
            objectiveFunction.RightSide.Where(term => !term.Constant).ForAll(term =>
            {
                var valueFromSolution = decisionVariableValues.Single(dvv => dvv.Variable.Index == term.Variable.Value.Index).Value;
                objFuncVal += term.SignedCoefficient * new Rational(valueFromSolution.Numerator, valueFromSolution.Denominator);
            });

            return new SimplexSolutionDto(new RationalDto { Numerator = objFuncVal.Numerator, Denominator = objFuncVal.Denominator }, decisionVariableValues);
        }

        /// <summary>
        /// Turns the LP model into standard form, so the model contains only inequations with <= direction, uses variables having zero lower bound and the aim is maximizing the objective functions value.
        /// </summary>
        /// <param name="model">The LP model wanted to be transformed to standard form.</param>
        /// <returns>The LP model itself in standard form.</returns>
        public static LPModel AsStandard(this LPModel model)
        {
            #region transforming equations to inequations
            var eqConstraints = model.Constraints.Where(constraint => constraint.SideConnection == SideConnection.Equal).Copy();
            foreach (var constraint in eqConstraints)
            {
                model.Constraints.Add(
                    new Equation
                    {
                        LeftSide = constraint.LeftSide.ToList().Copy() as IList<Term>,
                        SideConnection = SideConnection.LessThanOrEqual,
                        RightSide = constraint.RightSide.ToList().Copy() as IList<Term>
                    }
                );
                model.Constraints.Add(
                    new Equation
                    {
                        LeftSide = constraint.LeftSide.ToList().Copy() as IList<Term>,
                        SideConnection = SideConnection.GreaterThanOrEqual,
                        RightSide = constraint.RightSide.ToList().Copy() as IList<Term>
                    }
                );
            }
            ((List<Equation>)model.Constraints).RemoveAll(constraint => constraint.SideConnection == SideConnection.Equal);
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
                    model.InterpretationRanges.Add(newVariable.GreaterOrEqualThanZeroRange());
                    model.StandardFormAliases.Add(alias);

                    // searching for the badly limited variable in the constraints and replacing them with the new one
                    foreach(var constraint in model.Constraints)
                    {
                        constraint.ReplaceVarWithExpression(variableAndRange.Key, alias.RightSide);
                    }
                    model.Objective.Function.ReplaceVarWithExpression(variableAndRange.Key, alias.RightSide);
                }
                else
                {
                    var newVariable1 = new Variable { Name = variableAndRange.Key.Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                    var newVariable2 = new Variable { Name = variableAndRange.Key.Name, Index = model.AllVariables.Max(var => var.Index) + 2 };
                    var alias = new Equation
                    {
                        // e.g. x1 = x2 - x3 (<limitless_var> = <zero_limit_var1> - <zero_limit_var2>
                        LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = variableAndRange.Key } },
                        SideConnection = SideConnection.Equal,
                        RightSide = new Term[] { new Term { SignedCoefficient = 1, Variable = newVariable1 }, new Term { SignedCoefficient = -1, Variable = newVariable2 } }
                    };

                    model.AllVariables.Add(newVariable1);
                    model.AllVariables.Add(newVariable2);
                    model.InterpretationRanges.Add(newVariable1.GreaterOrEqualThanZeroRange());
                    model.InterpretationRanges.Add(newVariable2.GreaterOrEqualThanZeroRange());
                    model.StandardFormAliases.Add(alias);

                    // searching for the limitless variable in the constraints and replacing them with the new expression
                    foreach (var constraint in model.Constraints)
                    {
                        constraint.ReplaceVarWithExpression(variableAndRange.Key, alias.RightSide);
                    }
                    model.Objective.Function.ReplaceVarWithExpression(variableAndRange.Key, alias.RightSide);
                }
            }
            #endregion

            #region transforming inequations with constraint >= to new ones having the constraint <=
            model.Constraints.Where(constraint => constraint.SideConnection == SideConnection.GreaterThanOrEqual).ForAll(constraint => constraint.Multiply(-1));
            #endregion

            #region changing the optimization aim to max if it was min
            model.ChangeOptimizationAimTo(OptimizationAim.Maximize);
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
            model.Constraints.ForAll(constraint =>
            {
                var newSlackVariable = new Variable { Name = model.AllVariables.First().Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                // this line transfers the terms of the left side to the right side
                constraint.Add(constraint.Copy().LeftSide.Multiply(-1));
                constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = 1, Variable = newSlackVariable } });
                constraint.SideConnection = SideConnection.Equal;

                model.AllVariables.Add(newSlackVariable);
                model.InterpretationRanges.Add(newSlackVariable.GreaterOrEqualThanZeroRange());
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
                var originalRightSide = constraint.RightSide.Where(term => term.Constant).ToList();
                // left side <= right side
                var originalSideConnection = SideConnection.LessThanOrEqual;

                model.Constraints[i] = new Equation
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
            model.InterpretationRanges.Add(var0.GreaterOrEqualThanZeroRange());

            model.Constraints.ForAll(constraint =>
            {
                constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = -1, Variable = var0 } });

                var newSlackVariable = new Variable { Name = model.AllVariables.First().Name, Index = model.AllVariables.Max(var => var.Index) + 1 };
                model.AllVariables.Add(newSlackVariable);
                model.InterpretationRanges.Add(newSlackVariable.GreaterOrEqualThanZeroRange());

                constraint.AddToLeft(new Term[] { new Term { SignedCoefficient = 1, Variable = newSlackVariable } });
                constraint.SideConnection = SideConnection.Equal;
            });
            #endregion

            #region Expressing the var0 variable from the constraint which has the most negative right side
            var mostNegativeRightSidedConstraint = model.Constraints.OrderBy(constraint => constraint.RightSide.SingleOrDefault(term => term.Constant)?.SignedCoefficient ?? 0).First();
            // on the right side there must be only one single constant (and nothing else) anyway
            var rightSideConstant = mostNegativeRightSidedConstraint.RightSide.Single(term => term.Constant);

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
                    var constantOnRight = constraint.RightSide.SingleOrDefault(term => term.Constant) ?? new Term { SignedCoefficient = 0 };

                    constraint.Add(new Term[] { new Term { SignedCoefficient = slackVariableTerm.SignedCoefficient * -1, Variable = slackVariableTerm.Variable } });
                    constraint.Add(new Term[] { new Term { SignedCoefficient = constantOnRight.SignedCoefficient * -1 } });
                    constraint.ChangeSides();
                    // TODO: le kell cserélni a reciprokkal történő szorzást osztásra
                    constraint.Multiply(1 / constraint.LeftSide.Single().SignedCoefficient);

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
                       LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = new Variable { Name = model.FirstPhasefunctionVariableName.ToString(), Index = 0 } } },
                       SideConnection = SideConnection.Equal,
                       RightSide = mostNegativeRightSidedConstraint.Copy().RightSide.Multiply(-1) as IList<Term>
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
                                                                           (constraint.RightSide.Count == 1 || constraint.RightSide.Any(term => term.Constant && term.SignedCoefficient == 0)) || constraint.RightSide.Count == 0)
                                                      .SingleOrDefault();
            if(constraintToRemove != null)
            {
                model.Constraints.Remove(constraintToRemove);
            }
            #endregion

            #region If var0 is a basis variable it will be exchanged with a non-basis variable by a pivot step
            var constraintWithVar0Basis = model.Constraints.Where(constraint => constraint.LeftSide.Count == 1 && constraint.LeftSide.Any(term => term.Variable.Value.Index == 0))
                                                           .SingleOrDefault();

            var var0 = new Variable { Name = model.AllVariables.First().Name, Index = 0 };
            if (constraintWithVar0Basis != null)
            {
                // choosing the variable has a negative coefficient and the smallest index
                var newBasisVariable = constraintWithVar0Basis.RightSide
                    .Where(term => term.Variable.Value.Index == constraintWithVar0Basis.RightSide
                        .Where(t => t.Variable.HasValue /*&& t.SignedCoefficient < 0*/)
                        .Min(t => t.Variable.Value.Index))
                    .Single().Variable.Value;

                model.MakePivotStep(newBasisVariable, var0, withoutObjectiveFunction: true);
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
            model.InterpretationRanges.Remove(model.InterpretationRanges.Single(range => range.LeftSide.Single().Variable?.Equals(var0) ?? false));
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
        /// Changes the LP models aim to the specified <see cref="OptimizationAim"/>.
        /// </summary>
        /// <param name="model">The LP model whose optimization aim will be changed.</param>
        /// <returns>The LP model having the selected optimization aim.</returns>
        internal static LPModel ChangeOptimizationAimTo(this LPModel model, OptimizationAim aim)
        {
            if (model.Objective.Aim != aim)
            {
                model.Objective.Aim = aim;
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
                    range => range.LeftSide.Any(term => term.Variable?.Equals(variable) ?? false)
                );
                if (withNoLimit)
                {
                    result.Add(variable, null);
                }
                else
                {
                    // variable with non-zero lower limit?
                    var nonZeroInterpretationRangeOfVariable = model.InterpretationRanges
                        .Where(range => (range.LeftSide.Single().Variable.Value.Equals(variable)) && // the variable can be found
                                        (range.RightSide.Single().Constant && range.RightSide.Single().SignedCoefficient != 0)) // the limit is a non-zero constant
                        .FirstOrDefault();
                    if (nonZeroInterpretationRangeOfVariable != null)
                    {
                        result.Add(variable, nonZeroInterpretationRangeOfVariable);
                    }
                    // else the variable has a zero lower limit - won't add to the collection
                }
            }

            return result;
        }

        /// <summary>
        /// Decides wheter a first phase is needed or not on the current dictionary form LP model.
        /// </summary>
        /// <param name="model">The LP model in dictionary form.</param>
        /// <returns>Wheter a first phase is needed or not.</returns>
        private static bool FirstPhaseNeeded(this LPModel model) => model.Constraints.Any(constraint => constraint.RightSide.Any(term => term.Constant && term.SignedCoefficient < 0));

        /// <summary>
        /// Runs the simplex algoritm on the given LP model.
        /// </summary>
        /// <param name="model">The LP model on which the simplex algoritm will be executed.</param>
        /// <returns></returns>
        /// <exception cref="SimplexAlgorithmExectionException"></exception>
        public static LPModel RunSimplex(this LPModel model)
        {
            while (!model.AllObjectiveFunctionVariableHasNegativeCoefficient())
            {
                #region Choosing new basis variable by Bland - let1s name its index as k
                var kIndex = model.Objective.Function.RightSide.Where(term => term.Variable.HasValue && term.SignedCoefficient > 0).Min(term => term.Variable.Value.Index);
                var stepInVariable = model.Objective.Function.RightSide.Where(term => term.Variable.HasValue && term.Variable.Value.Index == kIndex).Single().Variable.Value;
                #endregion

                #region All k-indexed variable has positive coefficient in the constraints? YES - stop, no limit, NO - continue
                Func<Term, bool> hasKIndex = term => term.Variable?.Equals(stepInVariable) ?? false;
                IEnumerable<Term> termsWithKIndexedVariables = model.Constraints.Where(constraint => constraint.RightSide.Any(term => hasKIndex(term)))
                    .Select(constraint => constraint.RightSide.Where(term => hasKIndex(term)).Single());

                bool allKIndexedHasPositiveIndex = termsWithKIndexedVariables.All(term => term.SignedCoefficient > 0);
                if (allKIndexedHasPositiveIndex)
                {
                    throw new SimplexAlgorithmExectionException(SimplexAlgorithmExectionErrorType.NotLimited);
                }
                #endregion

                #region Determining the variable which will leave the base by finding the smallest quotient
                Equation selectedConstraint = null;
                Rational? smallestQuotient = null;
                model.Constraints.Where(constraint => constraint.RightSide.Any(term => hasKIndex(term) && term.SignedCoefficient < 0))
                    .ForAll(constraint =>
                    {
                        // This value must be non-negative - if not the dictionary were not valid
                        var constraintsConstantValue = constraint.RightSide.Where(term => term.Constant).SingleOrDefault()?.SignedCoefficient ?? 0;
                        var kIndexedVariablesCoefficient = constraint.RightSide.Where(term => hasKIndex(term)).Single().SignedCoefficient;
                        // If this is the fist ratio, we will store it automatically (smallestQuotient doesn't have a value yet
                        var smallestQuotientFound = !smallestQuotient.HasValue || ((constraintsConstantValue / kIndexedVariablesCoefficient.Abs()) < smallestQuotient);
                        if (smallestQuotientFound)
                        {
                            selectedConstraint = constraint;
                            smallestQuotient = constraintsConstantValue / kIndexedVariablesCoefficient.Abs();
                        }
                    });
                var stepOutVariable = selectedConstraint.LeftSide.Single().Variable.Value;
                #endregion

                #region Making a pivot step
                model.MakePivotStep(stepInVariable, stepOutVariable);
                #endregion
            }

            return model;
        }

        /// <summary>
        /// Checks wheter only negative coefficients can be found in the objective function or not. If so the execution of the simplex algoritm can be finished.
        /// </summary>
        /// <param name="model">The LP model on which the check will be perfomed.</param>
        /// <returns>Wheter the execution of the simplex algoritm can be finished or not.</returns>
        private static bool AllObjectiveFunctionVariableHasNegativeCoefficient(this LPModel model) => model.Objective.Function.RightSide.Where(term => term.Variable.HasValue).All(term => term.SignedCoefficient < 0);

        /// <summary>
        /// Does a pivot step by the specified variables on the given LP model.
        /// </summary>
        /// <param name="model">The LP mondel on which the pivot step will be done.</param>
        /// <param name="stepInVariable">This non-basis variable will step in the base.</param>
        /// <param name="stepOutVariable">This basis variable will step out from the base.</param>
        /// <param name="withoutObjectiveFunction">Optional - wheter the new basis varibale should be exchanged with its equivalent (right side of its constraint) in the objective function or not.</param>
        /// <returns>The model after the pivot step.</returns>
        private static LPModel MakePivotStep(this LPModel model, Variable stepInVariable, Variable stepOutVariable, bool withoutObjectiveFunction = false)
        {
            var constraintWithStepOutBasisVariable = model.Constraints.Where(constraint => constraint.LeftSide.Single().Variable?.Equals(stepOutVariable) ?? false).Single();
            var stepInVariableTerm = constraintWithStepOutBasisVariable.RightSide.Where(term => term.Variable?.Equals(stepInVariable) ?? false).Single();

            constraintWithStepOutBasisVariable.Add(new Term[] { 
                new Term { SignedCoefficient = -1, Variable = stepOutVariable },
                new Term { SignedCoefficient = stepInVariableTerm.SignedCoefficient * -1, Variable = stepInVariable }
            });
            // TODO: le kell cserélni a reciprokkal történő szorzást osztásra
            constraintWithStepOutBasisVariable.Multiply(new Rational(stepInVariableTerm.SignedCoefficient.Denominator, stepInVariableTerm.SignedCoefficient.Numerator) * -1);

            model.Constraints.Where(constraint => constraint != constraintWithStepOutBasisVariable)
                    .ForAll(constraint => constraint.ReplaceVarWithExpression(stepInVariable, constraintWithStepOutBasisVariable.RightSide));

            if (!withoutObjectiveFunction)
            {
                // TODO: the right side will be copied in the replace function
                model.Objective.Function.ReplaceVarWithExpression(stepInVariable, constraintWithStepOutBasisVariable.RightSide);
            }

            return model;
        }

        /// <summary>
        /// Decides if there is at least one row in the dictionary whose constant is negative or not.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static bool AllBasisVariableHaveNonNegativeValuesInTheDictionary(this LPModel model) => !model.Constraints.Where(dictionaryRow => (dictionaryRow.RightSide.SingleOrDefault(term => term.Constant)?.SignedCoefficient ?? 0) < 0).Any();
    }
}
