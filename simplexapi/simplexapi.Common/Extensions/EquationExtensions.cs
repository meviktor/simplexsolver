using simplexapi.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.Extensions
{
    public static class EquationExtensions
    {
        public static Equation Multiply(this Equation equation, int factor)
        {
            equation.LeftSide.ForAll(term => term.SignedCoefficient *= factor);
            equation.RightSide.ForAll(term => term.SignedCoefficient *= factor);

            if (factor < 0)
            {
                switch (equation.SideConnection)
                {
                    case SideConnection.LessThan:
                        equation.SideConnection = SideConnection.GreaterThan;
                        break;
                    case SideConnection.LessThanOrEqual:
                        equation.SideConnection = SideConnection.GreaterThanOrEqual;
                        break;
                    case SideConnection.GreaterThanOrEqual:
                        equation.SideConnection = SideConnection.LessThanOrEqual;
                        break;
                    case SideConnection.GreaterThan:
                        equation.SideConnection = SideConnection.LessThan;
                        break;
                    default:
                        break;
                }
            }

            return equation;
        }

        public static Equation Add(this Equation equation, IEnumerable<Term> termsToAdd)
        {
            equation.AddToLeft(termsToAdd);
            equation.AddToRight(termsToAdd);

            return equation;
        }

        public static Equation AddToLeft(this Equation equation, IEnumerable<Term> termsToAdd)
        {
            foreach (var term in termsToAdd)
            {
                equation.LeftSide.Add(term);
            }
            equation.Merge(Side.Left);

            return equation;
        }

        public static Equation AddToRight(this Equation equation, IEnumerable<Term> termsToAdd)
        {
            foreach (var term in termsToAdd)
            {
                equation.RightSide.Add(term);
            }
            equation.Merge(Side.Right);

            return equation;
        }

        private static void Merge(this Equation equation, Side mergeSide)
        {
            IEnumerable<Term> side = mergeSide == Side.Left ? equation.LeftSide : equation.RightSide;

            List<Term> result = new List<Term>();
            foreach (var termToMerge in side)
            {

                bool termAlreadyMerged = result.Any(mergedTerm => mergedTerm.Variable?.Equals(termToMerge.Variable) ?? !termToMerge.Variable.HasValue);
                if (!termAlreadyMerged)
                {
                    var coefficientAfterMerge = side.Where(term => term.Variable?.Equals(termToMerge.Variable) ?? !termToMerge.Variable.HasValue).Sum(term => term.SignedCoefficient);
                    if(coefficientAfterMerge != 0)
                    {
                        result.Add(new Term
                        {
                            SignedCoefficient = coefficientAfterMerge,
                            Variable = termToMerge.Variable
                        });
                    }
                }
            }

            if (mergeSide == Side.Left)
            {
                equation.LeftSide = result;
            }
            else 
            {
                equation.RightSide = result; 
            }
        }
    }
}