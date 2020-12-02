using simplexapi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.Models
{
    public class LPModelDto
    {
        public char DecisionVariableName { get; set; }

        public char FunctionVariableName { get; set; }

        public char FirstPhaseFunctionVariableName { get; set; }
        
        public uint NumberOfDecisionVariables { get; set; }

        public uint NumberOfConstraints { get; set; }

        public int[][] ConstraintsLeftSideMatrix { get; set; }

        public SideConnection[] ConstraintConnectionsVector { get; set; }
      
        public int[] ConstraintsRightVector { get; set; }

        public int?[] InterpretationRanges { get; set; }

        public bool Maximization { get; set; }

        public int[] ObjectiveCoefficientVector { get; set; }
    }

    public static class LPModelDtoExtensions
    {
        public static void Validate(this LPModelDto dto)
        {
            if (dto.DecisionVariableName < 'a' || dto.DecisionVariableName > 'z' || dto.DecisionVariableName == dto.FunctionVariableName || dto.DecisionVariableName == dto.FirstPhaseFunctionVariableName)
            {
                throw new ArgumentException(nameof(dto.DecisionVariableName));
            }

            if (dto.FunctionVariableName < 'a' || dto.FunctionVariableName > 'z' || dto.FunctionVariableName == dto.DecisionVariableName || dto.FunctionVariableName == dto.FirstPhaseFunctionVariableName)
            {
                throw new ArgumentException(nameof(dto.FunctionVariableName));
            }

            if (dto.FirstPhaseFunctionVariableName < 'a' || dto.FirstPhaseFunctionVariableName > 'z' || dto.FirstPhaseFunctionVariableName == dto.DecisionVariableName || dto.FirstPhaseFunctionVariableName == dto.FunctionVariableName)
            {
                throw new ArgumentException(nameof(dto.FirstPhaseFunctionVariableName));
            }

            if (dto.NumberOfDecisionVariables == 0)
            {
                throw new ArgumentException(nameof(dto.NumberOfDecisionVariables));
            }

            if (dto.NumberOfConstraints == 0)
            {
                throw new ArgumentException(nameof(dto.NumberOfConstraints));
            }

            if (dto.ConstraintsLeftSideMatrix.Length != dto.NumberOfConstraints || dto.ConstraintsLeftSideMatrix.Any(constraintRow => constraintRow.Length != dto.NumberOfDecisionVariables))
            {
                throw new ArgumentException(nameof(dto.ConstraintsLeftSideMatrix));
            }

            if (dto.ConstraintConnectionsVector.Length != dto.NumberOfConstraints || dto.ConstraintConnectionsVector.Any(connection => !Enum.IsDefined(typeof(SideConnection), connection)))
            {
                throw new ArgumentException(nameof(dto.ConstraintConnectionsVector));
            }

            if (dto.ConstraintsRightVector.Length != dto.NumberOfConstraints)
            {
                throw new ArgumentException(nameof(dto.ConstraintsRightVector));
            }

            if (dto.InterpretationRanges.Length != dto.NumberOfDecisionVariables)
            {
                throw new ArgumentException(nameof(dto.InterpretationRanges));
            }

            if (dto.ObjectiveCoefficientVector.Length != dto.NumberOfDecisionVariables)
            {
                throw new ArgumentException(nameof(dto.ObjectiveCoefficientVector));
            }
        }

        public static LPModel MapTo(this LPModelDto dto, LPModel model)
        {
            model.DecisionVariableName = dto.DecisionVariableName;
            model.FunctionVariableName = dto.FunctionVariableName;
            model.FirstPhasefunctionVariableName = dto.FirstPhaseFunctionVariableName;

            Func<IEnumerable<Variable>> decisionVarExpr = () => Enumerable.Range(1, (int)dto.NumberOfDecisionVariables).Select(i => new Variable { Name = dto.DecisionVariableName.ToString(), Index = (uint)i });
            model.DecisionVariables = decisionVarExpr().ToArray();
            model.AllVariables = decisionVarExpr().ToList();

            model.Constraints = new List<Equation>();
            Enumerable.Range(0, (int)dto.NumberOfConstraints).ForAll(i =>
            {
                List<Term> leftSide = new List<Term>();
                Enumerable.Range(0, (int)dto.NumberOfDecisionVariables).ForAll(k =>
                {
                    if(dto.ConstraintsLeftSideMatrix[i][k] != 0)
                    {
                        leftSide.Add(new Term { SignedCoefficient = dto.ConstraintsLeftSideMatrix[i][k], Variable = new Variable { Name = dto.DecisionVariableName.ToString(), Index = (uint)k + 1} });
                    }
                });

                model.Constraints.Add(new Equation
                {
                    LeftSide = leftSide,
                    RightSide = new List<Term>(new Term[] { new Term { SignedCoefficient = dto.ConstraintsRightVector[i] } }),
                    SideConnection = dto.ConstraintConnectionsVector[i]
                });
            });

            model.InterpretationRanges = new List<Equation>();
            Enumerable.Range(0, (int)dto.NumberOfDecisionVariables).ForAll(i =>
            {
                if(dto.InterpretationRanges[i] != null)
                {
                    model.InterpretationRanges.Add(new Equation
                    {
                        LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = new Variable { Name = dto.DecisionVariableName.ToString(), Index = (uint)i + 1} } },
                        RightSide = new Term[] { new Term { SignedCoefficient = dto.InterpretationRanges[i].Value } },
                        SideConnection = SideConnection.GreaterThanOrEqual
                    });
                }
            });

            List<Term> objectiveRightSide = new List<Term>();
            Enumerable.Range(0, (int)dto.NumberOfDecisionVariables).ForAll(k =>
            {
                if(dto.ObjectiveCoefficientVector[k] != 0)
                {
                    objectiveRightSide.Add(new Term { SignedCoefficient = dto.ObjectiveCoefficientVector[k], Variable = new Variable { Name = dto.DecisionVariableName.ToString(), Index = (uint)k + 1} });
                }
            });
            model.Objective = new Objective(
                dto.Maximization ? OptimizationAim.Maximize : OptimizationAim.Minimize,
                new Equation
                {
                    LeftSide = new Term[] {new Term { SignedCoefficient = 1, Variable = new Variable { Name = dto.FunctionVariableName.ToString(), Index = 0 } } },
                    RightSide = objectiveRightSide,
                    SideConnection = SideConnection.Equal
                }
            );

            return model;
        }
    }
}