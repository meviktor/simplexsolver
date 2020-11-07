using System;

namespace simplexapi.Common.Exceptions
{
    public class SimplexAlgorithmExectionException : Exception
    {
        public SimplexAlgorithmExectionErrorType ExecutionError { get; private set; }

        public SimplexAlgorithmExectionException(SimplexAlgorithmExectionErrorType errorType) 
        {
            ExecutionError = errorType;
        }

        public SimplexAlgorithmExectionException(SimplexAlgorithmExectionErrorType errorType, string message) : base(message)
        {
            ExecutionError = errorType;
        }
    }

    public enum SimplexAlgorithmExectionErrorType
    {
        NoSolution,
        NotLimited
    }
}
