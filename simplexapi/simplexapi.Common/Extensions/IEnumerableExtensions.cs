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
        public static IEnumerable<Term> Multiply(this IEnumerable<Term> expression, double factor)
        {
            expression.ForAll(term => term.SignedCoefficient *= factor);
            return expression;
        }
    }
}
