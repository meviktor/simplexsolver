using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace simplexapi.Common.Models
{
    public enum OptimizationAim
    {
        Minimize,
        Maximize
    }

    public class Objective
    {
        public OptimizationAim Aim { get; set; }
        public Variable FunctionName => Function.LeftSide.Single().Variable.Value;

        private Equation _function;
        public Equation Function {
            get
            {
                return _function;
            } 
            set
            {
                if (value.LeftSide.Count != 1 || value.LeftSide.Single().SignedCoefficient != 1 || !value.LeftSide.Single().Variable.HasValue) 
                {
                    throw new ArgumentException(nameof(Equation.LeftSide));
                }
                else
                {
                    _function = value;
                }
            }
        }

        public double FunctionValue => _function.RightSide.Where(term => !term.Variable.HasValue).SingleOrDefault()?.SignedCoefficient ?? 0;

        public Objective(OptimizationAim aim, Equation function)
        {
            Aim = aim;
            Function = function;
        }
    }
    
    public class LPModel
    {
        public Variable[] DecisionVariables { get; set; }
        public IList<Variable> AllVariables { get; set; }
        public IList<Equation> Constraints { get; set; }
        public IList<Equation> InterpretationRanges { get; set; }
        public IList<Equation> StandardFormAliases { get; set; }
        public Objective Objective { get; set; }
        public Objective TmpObjective { get; set; }

        public override string ToString()
        {
            var lpModelAsString = new StringBuilder();

            foreach(var constraint in Constraints)
            {
                lpModelAsString.Append($"{constraint}{Environment.NewLine}");
            }
            lpModelAsString.Append($"----{Environment.NewLine}");

            lpModelAsString.Append($"{Objective.Function}{Environment.NewLine}");
            lpModelAsString.Append($"----{Environment.NewLine}");

            lpModelAsString.Append("Interpretation ranges: ");
            for (int i = 0; i < InterpretationRanges.Count(); i++)
            {
                if (i != InterpretationRanges.Count() - 1)
                {
                    lpModelAsString.Append(string.Format("{0}, ", InterpretationRanges[i]));
                }
                else
                {
                    lpModelAsString.Append(InterpretationRanges[i]);
                }
            }
            lpModelAsString.Append(Environment.NewLine);

            lpModelAsString.Append("Decision variables: ");
            for (int i = 0; i < DecisionVariables.Count(); i++)
            {
                if (i != DecisionVariables.Count() - 1)
                {
                    lpModelAsString.Append(string.Format("{0}, ", DecisionVariables[i]));
                }
                else
                {
                    lpModelAsString.Append(DecisionVariables[i]);
                }
            }
            lpModelAsString.Append(Environment.NewLine);
            lpModelAsString.Append($"----{Environment.NewLine}");

            return lpModelAsString.ToString();
        }
    }
}