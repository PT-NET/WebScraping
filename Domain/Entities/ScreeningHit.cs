using WebScraping.Domain.Enums;

namespace WebScraping.Domain.Entities
{
    public class ScreeningHit
    {
        public string EntityName { get; set; } = string.Empty;
        public ScreeningSource Source { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new();
        public double? MatchScore { get; set; }
        public DateTime ScrapedAt { get; set; }

        public static ScreeningHit CreateOffshoreLeaksHit(
            string entityName,
            string jurisdiction,
            string linkedTo,
            string dataFrom)
        {
            return new ScreeningHit
            {
                EntityName = entityName,
                Source = ScreeningSource.OffshoreLeaks,
                Attributes = new Dictionary<string, string>
            {
                { "Entity", entityName },
                { "Jurisdiction", jurisdiction },
                { "LinkedTo", linkedTo },
                { "DataFrom", dataFrom }
            },
                ScrapedAt = DateTime.UtcNow
            };
        }

        public static ScreeningHit CreateWorldBankHit(
            string firmName,
            string address,
            string country,
            string fromDate,
            string toDate,
            string grounds)
        {
            return new ScreeningHit
            {
                EntityName = firmName,
                Source = ScreeningSource.WorldBank,
                Attributes = new Dictionary<string, string>
            {
                { "FirmName", firmName },
                { "Address", address },
                { "Country", country },
                { "FromDate", fromDate },
                { "ToDate", toDate },
                { "Grounds", grounds }
            },
                ScrapedAt = DateTime.UtcNow
            };
        }

        public static ScreeningHit CreateOfacHit(
            string name,
            string address,
            string type,
            string programs,
            string list,
            double score)
        {
            return new ScreeningHit
            {
                EntityName = name,
                Source = ScreeningSource.OFAC,
                Attributes = new Dictionary<string, string>
            {
                { "Name", name },
                { "Address", address },
                { "Type", type },
                { "Programs", programs },
                { "List", list }
            },
                MatchScore = score,
                ScrapedAt = DateTime.UtcNow
            };
        }
    }
}
