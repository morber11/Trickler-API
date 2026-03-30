namespace Trickler_API.Exceptions
{
    public class TricklerException : Exception
    {
        public TricklerException(string message) : base(message) { }
        public TricklerException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class TrickleNotFoundException(int trickleId) : TricklerException($"Trickle with ID {trickleId} not found")
    {
        public int TrickleId { get; } = trickleId;
    }

    public class AppValidationException(string message) : TricklerException(message)
    {
    }

    public class AuthenticationException(string message) : TricklerException(message)
    {
    }
}
