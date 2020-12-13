using System;

namespace simplexapi.Common.Models
{
    public struct Rational : IComparable<Rational>, IEquatable<Rational>
    {
        public int Numerator { get; private set; }

        public int Denominator { get; private set; }

        public double NumericValue => (double)Numerator / (double)Denominator;

        public Rational(int value)
        {
            Numerator = value;
            Denominator = 1;
        }

        public Rational(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                throw new ArgumentException(nameof(denominator));
            }
            Numerator = numerator;
            Denominator = denominator;
        }

        public bool Integer => this.Simplify().Denominator == 1;

        /// <summary>
        /// The numerator and the denominator have the same sign (positive or negative) or not.
        /// </summary>
        public bool SameSigned => (Numerator >= 0 && Denominator > 0) || (Numerator < 0 && Denominator < 0);

        public static Rational Zero => new Rational(0);

        public static implicit operator Rational(int i) => new Rational(i);

        public static Rational operator -(Rational a) => new Rational(-a.Numerator, a.Denominator);
        public static Rational operator +(Rational a, Rational b) => new Rational(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator).Simplify();
        public static Rational operator +(Rational a, int b) => a + new Rational(b * a.Denominator, a.Denominator);
        public static Rational operator -(Rational a, Rational b) => a + (-b);
        public static Rational operator -(Rational a, int b) => a + (-b);
        public static Rational operator *(Rational a, Rational b) => new Rational(a.Numerator * b.Numerator, a.Denominator * b.Denominator).Simplify();
        public static Rational operator *(Rational a, int b) => new Rational(b * a.Numerator, a.Denominator);
        public static Rational operator /(Rational a, Rational b) => new Rational(a.Numerator * b.Denominator, a.Denominator * b.Numerator).Simplify();
        public static Rational operator /(Rational a, int b) => new Rational(a.Numerator, b * a.Denominator).Simplify();
        public static bool operator ==(Rational a, int b) => a.NumericValue == b;
        public static bool operator !=(Rational a, int b) => a.NumericValue != b;
        public static bool operator ==(Rational a, double b) => a.NumericValue == b;
        public static bool operator !=(Rational a, double b) => a.NumericValue != b;

        public static bool operator ==(Rational a, Rational b)
        {
            var (a1, b1) = ToCommonDenominator(a, b);
            return a1.Numerator == b1.Numerator;
        }
        public static bool operator !=(Rational a, Rational b)
        {
            var (a1, b1) = ToCommonDenominator(a, b);
            return a1.Numerator != b1.Numerator;
        }

        public static bool operator <(Rational a, int b) => a.NumericValue < b;
        public static bool operator >(Rational a, int b) => a.NumericValue > b;
        public static bool operator <(Rational a, double b) => a.NumericValue < b;
        public static bool operator >(Rational a, double b) => a.NumericValue > b;

        public static bool operator <(Rational a, Rational b)
        {
            var (a1, b1) = ToCommonDenominator(a, b);
            return (a1.Denominator > 0) ? (a1.Numerator < b1.Numerator) : (a1.Numerator > b1.Numerator);
        }
        public static bool operator >(Rational a, Rational b)
        {
            var (a1, b1) = ToCommonDenominator(a, b);
            return (a1.Denominator > 0) ? (a1.Numerator > b1.Numerator) : (a1.Numerator < b1.Numerator);
        }

        public static bool operator <=(Rational a, int b) => a.NumericValue <= b;
        public static bool operator >=(Rational a, int b) => a.NumericValue >= b;
        public static bool operator <=(Rational a, double b) => a.NumericValue <= b;
        public static bool operator >=(Rational a, double b) => a.NumericValue >= b;

        public static bool operator <=(Rational a, Rational b)
        {
            var (a1, b1) = ToCommonDenominator(a, b);
            return (a1.Denominator > 0) ? (a1.Numerator <= b1.Numerator) : (a1.Numerator >= b1.Numerator);
        }
        public static bool operator >=(Rational a, Rational b)
        {
            var (a1, b1) = ToCommonDenominator(a, b);
            return (a1.Denominator > 0) ? (a1.Numerator >= b1.Numerator) : (a1.Numerator <= b1.Numerator);
        }

        private static Tuple<Rational, Rational> ToCommonDenominator(Rational a, Rational b) => new Tuple<Rational, Rational>(
            new Rational(a.Numerator * b.Denominator, a.Denominator * b.Denominator),
            new Rational(b.Numerator * a.Denominator, a.Denominator * b.Denominator)
        );

        public override string ToString() => NumericValue % 1 == 0 ? NumericValue.ToString("+#;-#;0") : $"{(SameSigned ? "+" : "-")}({Math.Abs(Numerator)}/{Math.Abs(Denominator)})";

        public override bool Equals(object obj) => obj is Rational ? Equals((Rational)obj) : false;

        public bool Equals(Rational other)
        {
            var simpleThis = this.Simplify();
            var simpleOther = other.Simplify();
            return simpleThis.Numerator == simpleOther.Numerator && simpleThis.Denominator == simpleOther.Denominator;
        }

        public int CompareTo(Rational other) => (this < other) ? -1 : ((this == other) ? 0 : 1);
    }

    public static class RationalExtensions
    {
        public static Rational Abs(this Rational rational) => new Rational(Math.Abs(rational.Numerator), Math.Abs(rational.Denominator));

        public static Rational Simplify(this Rational rational)
        {
            uint greatestCommonDivisor = Euclidean((uint)Math.Abs(rational.Numerator), (uint)Math.Abs(rational.Denominator));
            var simplest = new Rational((int)(rational.Numerator / greatestCommonDivisor), (int)(rational.Denominator / greatestCommonDivisor));
            return simplest.SameSigned ? simplest.Abs() :
                simplest.Denominator < 0 ? new Rational(-simplest.Numerator, -simplest.Denominator) : simplest;
        }

        private static uint Euclidean(uint a, uint b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a | b;
        }
    }
}
