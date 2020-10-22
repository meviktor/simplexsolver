using NUnit.Framework;
using simplexapi.Common.Extensions;
using simplexapi.Common.Models;
using System.Collections.Generic;

namespace simplexapi.Common.Test
{

    public class EquationExtensionTests
    {
        private Equation _eq;
        private IList<Term> _terms;
        private Term _constant;

        [SetUp]
        public void Setup()
        {
            _eq = new Equation
            {
                LeftSide = new List<Term> 
                {
                    new Term { SignedCoefficient = -3, Variable = new Variable { Name = "y", Index = 1} },
                    new Term { SignedCoefficient =  2, Variable = new Variable { Name = "y", Index = 2} },
                    new Term { SignedCoefficient =  1, Variable = new Variable { Name = "y", Index = 3} },
                    new Term { SignedCoefficient =  4, Variable = new Variable { Name = "y", Index = 4} },
                },
                SideConnection = SideConnection.LessThanOrEqual,
                RightSide = new List<Term> { new Term { SignedCoefficient = 5 } }
            };

            _terms = new List<Term>
            {
                new Term { SignedCoefficient =  1, Variable = new Variable { Name = "y", Index = 1} },
                new Term { SignedCoefficient = -2, Variable = new Variable { Name = "y", Index = 2} },
                new Term { SignedCoefficient =  5, Variable = new Variable { Name = "y", Index = 3} },
            };

            _constant = new Term { SignedCoefficient = -5 };
        }

        [Test]
        public void Extensions_Multiply_Positive()
        {
            Assert.AreEqual("-15y1 +10y2 +5y3 +20y4 <= +25", _eq.Multiply(5).ToString());
        }

        [Test]
        public void Extensions_Multiply_Negative()
        {
            Assert.AreEqual("+3y1 -2y2 -1y3 -4y4 >= -5", _eq.Multiply(-1).ToString());
        }

        [Test]
        public void Extensions_Add()
        {
            Assert.AreEqual("-2y1 +6y3 +4y4 <= +5 +1y1 -2y2 +5y3", _eq.Add(_terms).ToString());
        }

        [Test]
        public void Extensions_Add_Constant()
        {
            Assert.AreEqual("-5 -3y1 +2y2 +1y3 +4y4 <= 0", _eq.Add(new Term[] { _constant }).ToString());
        }

        [Test]
        public void Extensions_AddToLeft()
        {
            Assert.AreEqual("-2y1 +6y3 +4y4 <= +5", _eq.AddToLeft(_terms).ToString());
        }

        [Test]
        public void Extensions_AddToRight()
        {
            Assert.AreEqual("-3y1 +2y2 +1y3 +4y4 <= +5 +1y1 -2y2 +5y3", _eq.AddToRight(_terms).ToString());
        }
    }
}
