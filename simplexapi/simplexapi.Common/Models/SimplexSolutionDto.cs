using System.Collections.Generic;

namespace simplexapi.Common.Models
{
    public class SimplexSolutionDto
    {
        public RationalDto ObjectiveFunctionValue { get; private set; }

        public IEnumerable<VariableValuePairDto> DecisionVariablesAndValues { get; private set; }

        public SimplexSolutionDto(RationalDto objectiveFunctionValue, IEnumerable<VariableValuePairDto> decisionVariablesAndValues)
        {
            ObjectiveFunctionValue = objectiveFunctionValue;
            DecisionVariablesAndValues = decisionVariablesAndValues;
        }
    }

    public class VariableDto
    {
        public uint Index { get; set; }
        public string Name { get; set; }
    }

    public class RationalDto
    {
        public int Numerator { get; set; }
        public int Denominator { get; set; }
    }

    public class VariableValuePairDto
    {
        public VariableDto Variable { get; set; }
        public RationalDto Value { get; set; }
    }
}
