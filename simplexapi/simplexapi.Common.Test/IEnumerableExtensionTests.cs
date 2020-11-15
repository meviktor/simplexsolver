using NUnit.Framework;
using simplexapi.Common.Extensions;
using simplexapi.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.Test
{
    public class IEnumerableExtensionTests
    {
        private IEnumerable<Term> _expression;
        private Variable _x1, _x2, _x3;

        [SetUp]
        public void SetUp()
        {
            _x1 = new Variable { Name = "x", Index = 1 };
            _x2 = new Variable { Name = "x", Index = 2 };
            _x3 = new Variable { Name = "x", Index = 3 };

            _expression = new List<Term>()
            {
                new Term { SignedCoefficient =  2 },
                new Term { SignedCoefficient =  5, Variable = _x1 },
                new Term { SignedCoefficient = -4, Variable = _x2 },
                new Term { SignedCoefficient =  1, Variable = _x3 },
            };
        }

        [Test]
        public void Multiply_WithNegative()
        {
            _expression.Multiply(-2);

            var resultHasTheRightTerms = _expression.Count(term => !term.Variable.HasValue && term.SignedCoefficient == -4) == 1 &&
                _expression.Count(term => term.Variable.HasValue && term.SignedCoefficient == -10 && term.Variable.Value.Equals(_x1)) == 1 &&
                _expression.Count(term => term.Variable.HasValue && term.SignedCoefficient == 8 && term.Variable.Value.Equals(_x2)) == 1 &&
                _expression.Count(term => term.Variable.HasValue && term.SignedCoefficient == -2 && term.Variable.Value.Equals(_x3)) == 1;

            Assert.AreEqual(true, resultHasTheRightTerms);
        }

        [Test]
        public void Multiply_WithPositive()
        {
            _expression.Multiply(7);

            var resultHasTheRightTerms = _expression.Count(term => !term.Variable.HasValue && term.SignedCoefficient == 14) == 1 &&
                _expression.Count(term => term.Variable.HasValue && term.SignedCoefficient == 35 && term.Variable.Value.Equals(_x1)) == 1 &&
                _expression.Count(term => term.Variable.HasValue && term.SignedCoefficient == -28 && term.Variable.Value.Equals(_x2)) == 1 &&
                _expression.Count(term => term.Variable.HasValue && term.SignedCoefficient == 7 && term.Variable.Value.Equals(_x3)) == 1;

            Assert.AreEqual(true, resultHasTheRightTerms);
        }
    }
}
