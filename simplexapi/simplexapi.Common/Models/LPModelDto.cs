using simplexapi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simplexapi.Common.Models
{
    public class LPModelDto
    {
        private char _decisionVariableName;
        private char _functionVariableName;
        private char _firstPhasefunctionVariableName;
        private uint _numberOfDecisionVariables;
        private uint _numberOfConstraints;
        private int[][] _constraintsLeftMatrix;
        private SideConnection[] _constraintConnectionsVector;
        private int[] _constraintsRightVector;
        private int?[] _interpretationRanges;
        private int[] _objectiveCoefficients;

        public char DecisionVariableName
        {
            get { return _decisionVariableName; }
            set
            {
                if (value >= 'a' && value <= 'z' && value != _functionVariableName && value != _firstPhasefunctionVariableName)
                {
                    _decisionVariableName = value;
                }
                else throw new ArgumentException(nameof(DecisionVariableName));
            }
        }

        public char FunctionVariableName
        {
            get { return _functionVariableName; }
            set
            {
                if (value >= 'a' && value <= 'z' && value != _decisionVariableName && value != _firstPhasefunctionVariableName)
                {
                    _functionVariableName = value;
                }
                else throw new ArgumentException(nameof(FunctionVariableName));
            }
        }

        public char FirstPhaseFunctionVariableName
        {
            get { return _firstPhasefunctionVariableName; }
            set
            {
                if (value >= 'a' && value <= 'z' && value != _decisionVariableName && value != _functionVariableName)
                {
                    _firstPhasefunctionVariableName = value;
                }
                else throw new ArgumentException(nameof(FirstPhaseFunctionVariableName));
            }
        }
        public uint NumberOfDecisionVariables
        {
            get { return _numberOfDecisionVariables; }
            set
            {
                if (value == 0)
                {
                    throw new ArgumentException(nameof(NumberOfDecisionVariables));
                }
                _numberOfDecisionVariables = value;
            }
        }

        public uint NumberOfConstraints
        {
            get { return _numberOfConstraints; }
            set
            {
                if (value == 0)
                {
                    throw new ArgumentException(nameof(NumberOfConstraints));
                }
                _numberOfConstraints = value;
            }
        }

        public int[][] ConstraintsLeftSideMatrix
        {
            get { return _constraintsLeftMatrix; }
            set
            {
                if (value.Length != _numberOfConstraints || value.Any(constraintRow => constraintRow.Length != _numberOfDecisionVariables))
                {
                    throw new ArgumentException(nameof(ConstraintsLeftSideMatrix));
                }
                _constraintsLeftMatrix = value;
            }
        }

        public SideConnection[] ConstraintConnectionsVector
        {
            get { return _constraintConnectionsVector; }
            set
            {
                if (value.Length != _numberOfConstraints || value.Any(connection => !Enum.IsDefined(typeof(SideConnection), connection)))
                {
                    throw new ArgumentException(nameof(ConstraintConnectionsVector));
                }
                _constraintConnectionsVector = value;
            }
        }

        public int[] ConstraintsRightVector
        {
            get { return _constraintsRightVector; }
            set
            {
                if (value.Length != _numberOfConstraints)
                {
                    throw new ArgumentException(nameof(ConstraintsRightVector));
                }
                _constraintsRightVector = value;
            }
        }

        public int?[] InterpretationRanges
        {
            get { return _interpretationRanges; }
            set
            {
                if (value.Length != _numberOfDecisionVariables)
                {
                    throw new ArgumentException(nameof(InterpretationRanges));
                }
                _interpretationRanges = value;
            }
        }

        public bool Maximization { get; set; }

        public int[] ObjectiveCoefficientVector
        {
            get { return _objectiveCoefficients; }
            set
            {
                if (value.Length != _numberOfDecisionVariables)
                {
                    throw new ArgumentException(nameof(ObjectiveCoefficientVector));
                }
                _objectiveCoefficients = value;
            }
        }
    }

    public static class LPModelDtoExtensions
    {
        public static LPModel MapTo(this LPModelDto dto, LPModel model)
        {
            model.DecisionVariableName = dto.DecisionVariableName;
            model.FunctionVariableName = dto.FunctionVariableName;
            model.FirstPhasefunctionVariableName = dto.FirstPhaseFunctionVariableName;

            Func<IEnumerable<Variable>> decisionVarExpr = () => Enumerable.Range(1, (int)dto.NumberOfConstraints).Select(i => new Variable { Name = dto.DecisionVariableName.ToString(), Index = (uint)i });
            model.DecisionVariables = decisionVarExpr().ToArray();
            model.AllVariables = decisionVarExpr().ToList();

            model.Constraints = new List<Equation>();
            Enumerable.Range(0, (int)dto.NumberOfConstraints - 1).ForAll(i =>
            {
                List<Term> leftSide = new List<Term>();
                Enumerable.Range(0, (int)dto.NumberOfDecisionVariables).ForAll(k =>
                {
                    if(dto.ConstraintsLeftSideMatrix[i][k] != 0)
                    {
                        leftSide.Add(new Term { SignedCoefficient = dto.ConstraintsLeftSideMatrix[i][k], Variable = new Variable { Name = dto.DecisionVariableName.ToString(), Index = (uint)k } });
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
            Enumerable.Range(0, (int)dto.NumberOfDecisionVariables - 1).ForAll(i =>
            {
                if(dto.InterpretationRanges[i] != null)
                {
                    model.InterpretationRanges.Add(new Equation
                    {
                        LeftSide = new Term[] { new Term { SignedCoefficient = 1, Variable = new Variable { Name = dto.DecisionVariableName.ToString(), Index = (uint)i } } },
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
                    objectiveRightSide.Add(new Term { SignedCoefficient = dto.ObjectiveCoefficientVector[k], Variable = new Variable { Name = dto.DecisionVariableName.ToString(), Index = (uint)k } });
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
