using simplexapi.Common.Models;

namespace simplexapi.Common.Extensions
{
    public static class VariableExtensions
    {
        /// <summary>
        /// Returns an equation having the form 'x >= 0' where x is a variable. This equation can be used as an interpretation range for variables.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>An equation having the 'x >= 0' form (where x is a variable).</returns>
        public static Equation GreaterOrEqualThanZeroRange(this Variable variable)
        {
            return new Equation
            {
                LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = variable } },
                SideConnection = SideConnection.GreaterThanOrEqual,
                RightSide = new Term[] { new Term { SignedCoefficient = Rational.Zero } }
            };
        }
    }
}
