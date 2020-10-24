using simplexapi.Common.Models;
using System;
using System.Collections.Generic;

namespace simplexapi.Common.Extensions
{
    public static class IEnumerableExtensions
    {
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

        public static IEnumerable<Term> Multiply(this IEnumerable<Term> expression, int factor)
        {
            expression.ForAll(term => term.SignedCoefficient *= factor);
            return expression;
        }
    }
}
