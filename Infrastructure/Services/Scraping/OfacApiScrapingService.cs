using Microsoft.Extensions.Options;
using System.Text.Json;
using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.Exceptions;
using WebScraping.Domain.ValueObjects;
using WebScraping.Infrastructure.Configuration;

namespace WebScraping.Infrastructure.Services.Scraping
{
    public class OfacApiScrapingService : BaseScrapingService
    {
        private static readonly Dictionary<string, List<SanctionData>> SanctionsDatabase = new()
        {
            ["maduro"] = new()
        {
            new("Nicolas MADURO MOROS", "Venezuela", "Individual",
                "VENEZUELA, IRAN-CON-ARMS-EO", 98.5, "President of Venezuela"),
            new("Nicolas Ernesto MADURO GUERRA", "Venezuela", "Individual",
                "VENEZUELA", 92.0, "Son of Nicolas Maduro")
        },
            ["putin"] = new()
        {
            new("Vladimir Vladimirovich PUTIN", "Russia", "Individual",
                "UKRAINE-EO13661, RUSSIA-EO14024", 99.0, "President of Russian Federation")
        },
            ["kim"] = new()
        {
            new("KIM Jong Un", "North Korea", "Individual",
                "DPRK, DPRK2, DPRK3, DPRK4", 97.5, "Supreme Leader of North Korea")
        },
            ["assad"] = new()
        {
            new("Bashar al-ASSAD", "Syria", "Individual",
                "SYRIA-CAESAR", 98.0, "President of Syria")
        },
            ["rosneft"] = new()
        {
            new("ROSNEFT", "Russia", "Entity",
                "UKRAINE-EO13662", 95.0, "Russian Oil Company")
        },
            ["gazprom"] = new()
        {
            new("GAZPROM", "Russia", "Entity",
                "UKRAINE-EO13662", 94.0, "Russian Gas Company")
        },
            ["bank"] = new()
        {
            new("Bank Rossiya", "Russia", "Entity",
                "UKRAINE-EO13661", 90.0, "Russian Financial Institution"),
            new("Central Bank of Iran", "Iran", "Entity",
                "IRAN", 88.0, "Iranian Financial Institution")
        },
            ["hezbollah"] = new()
        {
            new("Hezbollah", "Lebanon", "Entity",
                "FTO, SDGT", 99.0, "Foreign Terrorist Organization")
        },
            ["taliban"] = new()
        {
            new("Taliban", "Afghanistan", "Entity",
                "SDGT, TALIBAN", 99.0, "Designated Terrorist Group")
        }
        };

        public override ScreeningSource Source => ScreeningSource.OFAC;

        public OfacApiScrapingService(
            ILogger<OfacApiScrapingService> logger,
            IOptions<ScrapingSettings> settings,
            HttpClient httpClient)
            : base(logger, settings, httpClient)
        {
        }

        public override async Task<List<ScreeningHit>> ScrapeAsync(
            EntityName entityName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Searching OFAC sanctions database (Mock Data) for entity: {EntityName}", entityName.Value);

                await Task.Delay(Random.Shared.Next(400, 1200), cancellationToken);

                var hits = new List<ScreeningHit>();
                var searchTerm = entityName.Value.ToLower();

                foreach (var (keyword, sanctions) in SanctionsDatabase)
                {
                    if (searchTerm.Contains(keyword))
                    {
                        foreach (var sanction in sanctions)
                        {
                            hits.Add(ScreeningHit.CreateOfacHit(
                                name: sanction.Name,
                                address: $"{sanction.Country} - {sanction.Description}",
                                type: sanction.Type,
                                programs: sanction.Programs,
                                list: "SDN",
                                score: sanction.MatchScore
                            ));
                        }
                    }
                }

                if (hits.Count == 0 && searchTerm.Length > 3 && !searchTerm.Contains("test") && !searchTerm.Contains("example"))
                {
                    foreach (var (keyword, sanctions) in SanctionsDatabase)
                    {
                        foreach (var sanction in sanctions)
                        {
                            if (sanction.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            {
                                hits.Add(ScreeningHit.CreateOfacHit(
                                    name: sanction.Name,
                                    address: $"{sanction.Country} - {sanction.Description}",
                                    type: sanction.Type,
                                    programs: sanction.Programs,
                                    list: "SDN",
                                    score: sanction.MatchScore * 0.85 
                                ));
                            }
                        }
                    }
                }

                Logger.LogInformation("Found {Count} hits in OFAC for {EntityName}", hits.Count, entityName.Value);
                Logger.LogWarning("NOTE: Using mock sanctions data based on real OFAC entries for demonstration purposes");

                return hits;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in OFAC search for entity: {EntityName}", entityName.Value);
                throw new ScrapingException(Source, ex.Message, ex);
            }
        }


        private record SanctionData(
        string Name,
        string Country,
        string Type,
        string Programs,
        double MatchScore,
        string Description);
    }
}