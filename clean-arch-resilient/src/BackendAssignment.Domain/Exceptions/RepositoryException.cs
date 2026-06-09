namespace BackendAssignment.Domain.Exceptions
{
    /// <summary>
    /// Custom exception for repository-related errors.
    /// </summary>
    public class RepositoryException : Exception
    {
        public int? ErrorCode { get; set; }

        public RepositoryException() { }

        public RepositoryException(string message)
            : base(message) { }

        public RepositoryException(string message, Exception innerException)
            : base(message, innerException) { }

        public RepositoryException(string message, int errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
