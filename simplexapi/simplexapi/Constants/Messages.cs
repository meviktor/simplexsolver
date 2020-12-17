namespace simplexapi.Constants
{
    public static class Messages
    {
        public const string SIMPLEX_RESULT_NO_SOLUTION = "The provided linear programming problem has no solution.";
        public const string SIMPLEX_RESULT_NO_LIMIT = "The provided linear programming problem has no limit.";

        public const string SIMPLEX_INT_RESULT_NO_SOLUTION = "The provided integer linear programming problem has no solution.";
        public const string SIMPLEX_INT_RESULT_NO_LIMIT = "The provided integer linear programming problem has no limit.";

        public const string WRONG_FORMAT_CHECK_ARG = "The provided linear programming model has a wrong format. Check the following field: {0}.";

        public const string GENERAL_ERROR = "An error occured while solving...";
    }
}
