using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.ValueObjects;

namespace WebScraping.Domain.Interfaces
{
    public interface IScrapingService
    {
        ScreeningSource Source { get; }
        Task<List<ScreeningHit>> ScrapeAsync(EntityName entityName, CancellationToken cancellationToken = default);
    }
}
