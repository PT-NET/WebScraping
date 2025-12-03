namespace WebScraping.Domain.Interfaces
{
    public interface IRateLimitService
    {
        Task<bool> IsAllowedAsync(string clientIdentifier, CancellationToken cancellationToken = default);
        Task<int> GetRemainingCallsAsync(string clientIdentifier, CancellationToken cancellationToken = default);
    }
}
