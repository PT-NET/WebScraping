namespace WebScraping.Application.DTOs
{
    public record ScreeningResponseDto
    {
        public string SearchedEntity { get; init; } = string.Empty;
        public int TotalHits { get; init; }
        public List<HitDto> Hits { get; init; } = new();
        public DateTime SearchedAt { get; init; }
        public double ExecutionTimeSeconds { get; init; }
        public List<string>? Errors { get; init; }
    }
}
