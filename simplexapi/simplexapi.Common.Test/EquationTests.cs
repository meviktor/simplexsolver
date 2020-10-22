using NUnit.Framework;
using simplexapi.Common.Models;
using System.Collections.Generic;

namespace simplexapi.Common.Test
{
    public class EquationTests
    {
        [Test]
        public void Variable_ToString()
        {
            var variable = new Variable() { Name = "x", Index = 1 };
            Assert.AreEqual("x1", variable.ToString());
        }

        [Test]
        public void Term_ToString()
        {
            var term = new Term() { SignedCoefficient = -3, Variable = new Variable() { Name = "y", Index = 2 } };
            Assert.AreEqual("-3y2", term.ToString());
        }

        [Test]
        public void Equation_ToString()
        {
            var eq = new Equation
            {
                LeftSide = new List<Term>()
                {
                    new Term() { SignedCoefficient = 3, Variable = new Variable() { Name = "x", Index = 1 } },
                    new Term() { SignedCoefficient = 1, Variable = new Variable() { Name = "x", Index = 2 } }
                },
                SideConnection = SideConnection.LessThanOrEqual,
                RightSide = new List<Term>()
                {
                    new Term { SignedCoefficient = 80 }
                }
            };
            Assert.AreEqual("+3x1 +1x2 <= +80", eq.ToString());
        }

        [Test]
        public void LPModel_ToString()
        {
            var constraint1 = new Equation
            {
                LeftSide = new List<Term>()
                {
                    new Term() { SignedCoefficient = 3, Variable = new Variable() { Name = "x", Index = 1 } },
                    new Term() { SignedCoefficient = 1, Variable = new Variable() { Name = "x", Index = 2 } }
                },
                SideConnection = SideConnection.LessThanOrEqual,
                RightSide = new List<Term>()
                {
                    new Term { SignedCoefficient = 80 }
                }
            };

            var constraint2 = new Equation
            {
                LeftSide = new List<Term>()
                {
                    new Term() { SignedCoefficient = 3, Variable = new Variable() { Name = "x", Index = 1 } },
                    new Term() { SignedCoefficient = -1, Variable = new Variable() { Name = "x", Index = 2 } }
                },
                SideConnection = SideConnection.GreaterThanOrEqual,
                RightSide = new List<Term>(){ }
            };

            var x1Ipr = new Equation
            {
                LeftSide = new List<Term>()
                {
                    new Term() { SignedCoefficient = 1, Variable = new Variable() { Name = "x", Index = 1 } },
                },
                SideConnection = SideConnection.GreaterThanOrEqual,
                RightSide = new List<Term>() { }
            };

            var x2Ipr = new Equation
            {
                LeftSide = new List<Term>()
                {
                    new Term() { SignedCoefficient = 1, Variable = new Variable() { Name = "x", Index = 2 } },
                },
                SideConnection = SideConnection.GreaterThanOrEqual,
                RightSide = new List<Term>() { }
            };

            var objective = new Objective(

                OptimizationAim.Maximize,
                new Equation
                {
                    LeftSide = new List<Term>()
                    {
                        new Term() { SignedCoefficient = 1, Variable = new Variable() { Name = "z", Index = 0 } },
                    },
                    SideConnection = SideConnection.Equal,
                    RightSide = new List<Term>()
                    {
                        new Term() { SignedCoefficient = 3, Variable = new Variable() { Name = "x", Index = 1 } },
                        new Term() { SignedCoefficient = 4, Variable = new Variable() { Name = "x", Index = 2 } }
                    }
                }
            );

            var lpModel = new LPModel
            {
                DecisionVariables = new Variable[] { new Variable() { Name = "x", Index = 1 }, new Variable() { Name = "x", Index = 2 } },
                AllVariables = new Variable[] { new Variable() { Name = "x", Index = 1 }, new Variable() { Name = "x", Index = 2 } },
                Constraints = new Equation[] { constraint1, constraint2 },
                InterpretationRanges = new Equation[] { x1Ipr, x2Ipr },
                Objective = objective
            };

            Assert.AreEqual(
                "+3x1 +1x2 <= +80\r\n+3x1 -1x2 >= 0\r\n----\r\n+1z0 = +3x1 +4x2\r\n----\r\nInterpretation ranges: +1x1 >= 0, +1x2 >= 0\r\nDecision variables: x1, x2\r\n----\r\n",
                lpModel.ToString()
            );
        }
    }
}