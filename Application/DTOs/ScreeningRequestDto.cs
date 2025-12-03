using WebScraping.Domain.Enums;

namespace WebScraping.Application.DTOs
{
    public record ScreeningRequestDto
    {
        public string EntityName { get; init; } = string.Empty;
        public List<ScreeningSource> Sources { get; init; } = new() { ScreeningSource.OffshoreLeaks };
    }
}
