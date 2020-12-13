using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace simplexapi.Common.Models
{
    public struct Variable : IEquatable<Variable>
    {
        public string Name { get; set; }
        public uint Index { get; set; }

        public bool Equals(Variable other) => other.Name == this.Name && other.Index == this.Index;

        public override string ToString()
        {
            return string.Format("{0}{1}", Name, Index);
        }
    }

    public class Term : IEquatable<Term>
    {
        public Rational SignedCoefficient { get; set; }
        // if this field is null the term is a constant
        public Variable? Variable { get; set; }

        public override string ToString()
        {
            return Variable.HasValue ?
                string.Format("{0}{1}", SignedCoefficient.ToString(), Variable) :
                string.Format("{0}", SignedCoefficient.ToString());
        }

        public bool Equals(Term other) => 
            SignedCoefficient.Equals(other.SignedCoefficient) &&
            ((this.Constant && other.Constant) || (!this.Constant && !other.Constant && this.Variable.Value.Equals(other.Variable.Value)));

        public bool Constant => !Variable.HasValue;
    }

    public enum SideConnection
    {
        LessThan,
        LessThanOrEqual,
        Equal,
        GreaterThanOrEqual,
        GreaterThan
    }

    public enum Side
    {
        Left,
        Right
    }

    public class Equation
    {
        public IList<Term> LeftSide { get; set; }
        public IList<Term> RightSide { get; set; }
        public SideConnection SideConnection { get; set; }

        public Equation()
        {
            LeftSide = new List<Term>();
            RightSide = new List<Term>();
            SideConnection = SideConnection.Equal;
        }

        public override string ToString()
        {
            var equationAsString = new StringBuilder();

            if (!LeftSide.Any())
            {
                equationAsString.Append(" 0");
            }
            else
            {
                // constant first, then by index asc
                var queuedTermsOnLeft = LeftSide.OrderBy(term => term.Variable.HasValue ? (int)term.Variable.Value.Index : -1);
                foreach (var term in queuedTermsOnLeft)
                {
                    if (!(term.SignedCoefficient == 0 && term.Variable.HasValue))
                    {
                        equationAsString.Append(string.Format(string.Format(" {0}", term)));
                    }
                }
            }

            switch (SideConnection)
            {
                case SideConnection.LessThan:
                    equationAsString.Append(" <");
                    break;
                case SideConnection.LessThanOrEqual:
                    equationAsString.Append(" <=");
                    break;
                case SideConnection.Equal:
                    equationAsString.Append(" =");
                    break;
                case SideConnection.GreaterThanOrEqual:
                    equationAsString.Append(" >=");
                    break;
                case SideConnection.GreaterThan:
                    equationAsString.Append(" >");
                    break;
                default:
                    break;
            }

            if (!RightSide.Any())
            {
                equationAsString.Append(" 0");
            }
            else
            {
                var queuedTermsOnRight = RightSide.OrderBy(term => term.Variable.HasValue ? (int)term.Variable.Value.Index : -1);
                foreach (var term in queuedTermsOnRight)
                {
                    if (!(term.SignedCoefficient == 0 && term.Variable.HasValue))
                    {
                        equationAsString.Append(string.Format(string.Format(" {0}", term)));
                    }
                }
            }

            return equationAsString.ToString().Trim();
        }
    }
}