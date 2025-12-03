using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.ValueObjects;

namespace WebScraping.Domain.Interfaces
{
    public interface IScreeningService
    {
        Task<ScreeningResult> ScreenEntityAsync(
        EntityName entityName,
        IEnumerable<ScreeningSource> sources,
        CancellationToken cancellationToken = default);
    }
}
