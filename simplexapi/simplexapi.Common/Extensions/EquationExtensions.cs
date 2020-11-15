using simplexapi.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.Extensions
{
    public static class EquationExtensions
    {
        public static Equation ReplaceVarWithExpression(this Equation equation, Variable varToReplace, IEnumerable<Term> aliasExpr)
        {
            var foundOnLeft = equation.LeftSide.SingleOrDefault(term => term.Variable?.Equals(varToReplace) ?? false);
            var foundOnRight = equation.RightSide.SingleOrDefault(term => term.Variable?.Equals(varToReplace) ?? false);

            if (foundOnLeft != null)
            {
                equation.AddToLeft(new Term[] { new Term { SignedCoefficient = foundOnLeft.SignedCoefficient * -1, Variable = varToReplace } });
                equation.AddToLeft(aliasExpr.Copy().Multiply(foundOnLeft.SignedCoefficient));
            }
            if(foundOnRight != null)
            {
                equation.AddToRight(new Term[] { new Term { SignedCoefficient = foundOnRight.SignedCoefficient * -1, Variable = varToReplace } });
                equation.AddToRight(aliasExpr.Copy().Multiply(foundOnRight.SignedCoefficient));
            }

            return equation;
        }

        public static Equation Multiply(this Equation equation, Rational factor)
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

        public static Equation ChangeSides(this Equation equation)
        {
            var leftSideTemp = equation.LeftSide;

            equation.LeftSide = equation.RightSide;
            equation.RightSide = leftSideTemp;

            return equation;
        }

        public static Equation DeepCopy(this Equation equation)
        {
            var leftSideCopy = new List<Term>();
            foreach(var term in equation.LeftSide)
            {
                leftSideCopy.Add(new Term
                {
                    SignedCoefficient = term.SignedCoefficient,
                    Variable = term.Variable
                });
            }

            var sideConnectionCopy = equation.SideConnection;

            var rightSideCopy = new List<Term>();
            foreach (var term in equation.RightSide)
            {
                rightSideCopy.Add(new Term
                {
                    SignedCoefficient = term.SignedCoefficient,
                    Variable = term.Variable
                });
            }

            return new Equation
            {
                LeftSide = leftSideCopy,
                SideConnection = sideConnectionCopy,
                RightSide = rightSideCopy
            };
        }
    }
}