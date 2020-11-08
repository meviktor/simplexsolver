using System.Collections.Generic;

namespace simplexapi.Common.Models
{
    public class SimplexSolutionDto
    {
        public double ObjectiveFunctionVariable { get; private set; }

        public Dictionary<Variable, double> DecisionVariablesAndValues { get; private set; }

        public SimplexSolutionDto(double objectiveFunctionValue, Dictionary<Variable, double> decisionVariablesAndValues)
        {
            ObjectiveFunctionVariable = objectiveFunctionValue;
            DecisionVariablesAndValues = decisionVariablesAndValues;
        }
    }
}
