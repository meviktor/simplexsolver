using simplexapi.Common.Models;
using System;
using System.Collections.Generic;

namespace simplexapi.Common.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Performs an action for all the elements can be found in the collection.
        /// </summary>
        /// <typeparam name="T">Type of the elements of the collection.</typeparam>
        /// <param name="source">The collection hving the elements on which the action will be performed.</param>
        /// <param name="action">The action to perform on the collection elements.</param>
        public static void ForAll<T>(this IEnumerable<T> source, Action<T> action)
        {
            if(source == null || action == null)
            {
                throw new ArgumentException(string.Format("Argument cannot be null. Argument name: '{0}'.", source == null ? nameof(source) : nameof(action)));
            }
            foreach(var element in source)
            {
                action(element);
            }
        }

        /// <summary>
        /// Multiplies an expression with a constant.
        /// </summary>
        /// <param name="expression">The expression wanted to be multiplied.</param>
        /// <param name="factor">The multiplication factor.</param>
        /// <returns>The result of the operation.</returns>
        public static IEnumerable<Term> Multiply(this IEnumerable<Term> expression, Rational factor)
        {
            expression.ForAll(term => term.SignedCoefficient *= factor);
            return expression;
        }

        /// <summary>
        /// Creates the deep copy of an expression.
        /// </summary>
        /// <param name="expression">The expression wanted to be copied.</param>
        /// <returns>The deep copy of the expression.</returns>
        public static IEnumerable<Term> Copy(this IEnumerable<Term> expression)
        {
            List<Term> result = new List<Term>();
            expression.ForAll(term => result.Add(new Term { SignedCoefficient = term.SignedCoefficient, Variable = term.Variable }));
            return result;
        }

        /// <summary>
        /// Creates the deep copy of an <see cref="Equation"></see> collection.
        /// </summary>
        /// <param name="expression">The expression wanted to be copied.</param>
        /// <returns>The deep copy of the expression.</returns>
        public static IEnumerable<Equation> Copy(this IEnumerable<Equation> expression)
        {
            List<Equation> result = new List<Equation>();
            expression.ForAll(equation => result.Add(new Equation
            {
                LeftSide = equation.LeftSide.Copy() as IList<Term>,
                RightSide = equation.RightSide.Copy() as IList<Term>,
                SideConnection = equation.SideConnection
            }));
            return result;
        }

        /// <summary>
        /// Summarizes a buch of <see cref="Rational"/> numbers selected by the selector expression.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="selector"></param>
        /// <returns>The sum of the selected elements.</returns>
        public static Rational Sum(this IEnumerable<Term> collection, Func<Term, Rational> selector)
        {
            Rational result = Rational.Zero;
            collection.ForAll(term => result += selector(term));
            return result;
        }
    }
}
