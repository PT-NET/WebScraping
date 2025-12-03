using WebScraping.Domain.ValueObjects;

namespace WebScraping.Domain.Entities
{
    public class ScreeningResult
    {
        public EntityName SearchedEntity { get; set; } = null!;
        public int TotalHits { get; set; }
        public List<ScreeningHit> Hits { get; set; } = new();
        public DateTime SearchedAt { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public List<string> Errors { get; set; } = new();

        public bool HasErrors => Errors.Any();
        public bool HasHits => TotalHits > 0;
    }
}
