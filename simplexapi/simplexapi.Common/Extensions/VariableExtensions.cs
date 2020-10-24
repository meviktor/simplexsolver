using simplexapi.Common.Models;

namespace simplexapi.Common.Extensions
{
    public static class VariableExtensions
    {
        public static Equation GreaterOrEqualThenZeroRange(this Variable variable)
        {
            return new Equation
            {
                LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = variable } },
                SideConnection = SideConnection.GreaterThanOrEqual,
                RightSide = new Term[] { new Term { SignedCoefficient = 0 } }
            };
        }
    }
}
