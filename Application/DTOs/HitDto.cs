namespace WebScraping.Application.DTOs
{
    public record HitDto
    {
        public string EntityName { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public Dictionary<string, string> Attributes { get; init; } = new();
        public double? MatchScore { get; init; }
    }
}
