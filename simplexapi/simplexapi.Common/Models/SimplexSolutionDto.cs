using System.Collections.Generic;

namespace simplexapi.Common.Models
{
    public class SimplexSolutionDto
    {
        public Rational ObjectiveFunctionValue { get; private set; }

        public Dictionary<Variable, Rational> DecisionVariablesAndValues { get; private set; }

        public SimplexSolutionDto(Rational objectiveFunctionValue, Dictionary<Variable, Rational> decisionVariablesAndValues)
        {
            ObjectiveFunctionValue = objectiveFunctionValue;
            DecisionVariablesAndValues = decisionVariablesAndValues;
        }
    }
}
