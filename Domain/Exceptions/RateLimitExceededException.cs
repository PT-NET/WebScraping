namespace WebScraping.Domain.Exceptions
{
    public class RateLimitExceededException : Exception
    {
        public int RetryAfterSeconds { get; }

        public RateLimitExceededException(int retryAfterSeconds)
            : base($"Rate limit exceeded. Please retry after {retryAfterSeconds} seconds.")
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
