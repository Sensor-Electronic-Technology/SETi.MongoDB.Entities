using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ardalis.GuardClauses;

namespace MongoDB.Entities;

public static partial class Extensions {
    public static void EmptyEnumerable<TSource>(this IGuardClause guardClause,
                                                IEnumerable<TSource> input, 
                                                [CallerArgumentExpression("input")] string? parameterName = null,
                                                [CallerArgumentExpression("message")] string? message = null) {
        if (input is null || !input.Any()) {
            throw new ArgumentException(message ?? "Enumerable cannot be an empty.", parameterName);
        }
    }
}