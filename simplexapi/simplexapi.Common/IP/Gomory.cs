using simplexapi.Common.Extensions;
using simplexapi.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.IP
{
    public static class Gomory
    {
        public static LPModel RunGomory(LPModelDto dto)
        {
            Func<LPModel, IEnumerable<Equation>> findEquationsWithFractionConstant = (lm) => lm.Constraints.Where(
                dictionaryRow => IsDecisionVariable(dictionaryRow.LeftSide.Single().Variable.Value, lm) &&
                                 !(dictionaryRow.RightSide.SingleOrDefault(term => term.Constant)?.SignedCoefficient.Integer ?? true) // if no contstant term can be found it is equivalent with the zero constant
            );
            LPModel lpModel;
            IEnumerable<Equation> equationsWithFractionConstant;
            IList<Equation> gomoryConstraints = new List<Equation>();

            lpModel = dto.MapTo(new LPModel());
            lpModel.TwoPhaseSimplex();

            // constraints are in dictionary form here... we need those rows where the contstant is not an integer
            equationsWithFractionConstant = findEquationsWithFractionConstant(lpModel);

            while (equationsWithFractionConstant.Any())
            {
                // its form like (dictionary row): <basis variable> = <constant> +/- <coefficient><non-basis variable> +/- ... +/- <coefficient><non-basis variable>
                var eqWithFracConst = equationsWithFractionConstant.First();
                gomoryConstraints.Add(MakeGomoryConstraint(eqWithFracConst));

                lpModel = dto.MapTo(new LPModel());
                gomoryConstraints.ForAll(constraint =>
                {
                    lpModel.Constraints.Add(constraint.Copy());
                    var newVariablesFromGomoryConstraint = constraint.LeftSide.Select(term => term.Variable).Where(variable => !lpModel.AllVariables.Contains(variable.Value));
                    newVariablesFromGomoryConstraint.ForAll(variable => 
                    { 
                        lpModel.AllVariables.Add(variable.Value);
                        lpModel.InterpretationRanges.Add(variable.Value.GreaterOrEqualThanZeroRange());
                    });
                });

                lpModel.DualSimplex();
                equationsWithFractionConstant = findEquationsWithFractionConstant(lpModel);
            }
            // only integers we are done...
            return lpModel;
        }

        /// <summary>
        /// Returns the fraction of a rational number. By definition it will be a non-negative 0 <= f < 1 value.
        /// E.g. for 5/4 f = 1/4, for -(5/4) f = 3/4.
        /// </summary>
        /// <param name="number">The number whose fraction will be returned</param>
        /// <returns>The fraction of the rational number.</returns>
        private static Rational GetFraction(Rational number) => number.NumericValue >= 0 ?
            new Rational(Math.Abs(number.Numerator) % Math.Abs(number.Denominator), Math.Abs(number.Denominator)) :
            1 - new Rational(Math.Abs(number.Numerator) % Math.Abs(number.Denominator), Math.Abs(number.Denominator));

        private static bool IsDecisionVariable(Variable var, LPModel model) => model.DecisionVariables.Contains(var);

        private static Equation MakeGomoryConstraint(Equation eqWithFracConst)
        {
            Equation newGomoryConstraint;
            // we "throw out" the basis variable from the left side and moving the non-constant terms from the right side to the left... then left side will look like this 
            var leftSideBase = eqWithFracConst.RightSide.Where(term => !term.Constant).Copy().Multiply(-1).ToList();
            // on the right side only the constant term could be found
            var rightSideBase = eqWithFracConst.RightSide.Where(term => term.Constant).Copy().ToList();

            leftSideBase.ForAll(term => term.SignedCoefficient = GetFraction(term.SignedCoefficient));
            rightSideBase.ForAll(term => term.SignedCoefficient = GetFraction(term.SignedCoefficient));

            newGomoryConstraint = new Equation
            {
                LeftSide = leftSideBase,
                RightSide = rightSideBase,
                SideConnection = SideConnection.GreaterThanOrEqual
            };
            newGomoryConstraint.Multiply(-1);

            return newGomoryConstraint;
        }
    }
}
