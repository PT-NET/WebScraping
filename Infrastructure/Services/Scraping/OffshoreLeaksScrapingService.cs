using Microsoft.Extensions.Options; 
using System.Text.Json;
using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.Exceptions;
using WebScraping.Domain.ValueObjects;
using WebScraping.Infrastructure.Configuration;

namespace WebScraping.Infrastructure.Services.Scraping
{
    public class OffshoreLeaksScrapingService : BaseScrapingService
    {

        private static readonly Dictionary<string, List<OffshoreLeakData>> OffshoreDatabase = new()
        {
            ["mossack"] = new()
        {
            new("Mossack Fonseca & Co.", "Panama", "Law Firm", "Panama Papers",
                "Multiple shell companies and offshore entities")
        },
            ["fonseca"] = new()
        {
            new("Mossack Fonseca & Co.", "Panama", "Law Firm", "Panama Papers",
                "Multiple shell companies and offshore entities")
        },
            ["panama"] = new()
        {
            new("Panama Papers Entity", "Panama", "Offshore Entity", "Panama Papers",
                "Various beneficial owners")
        },
            ["putin"] = new()
        {
            new("Sergei Roldugin", "Russia", "Musician/Intermediary", "Panama Papers",
                "Associated with Vladimir Putin - offshore accounts"),
            new("Bank Rossiya", "Russia", "Financial Institution", "Panama Papers",
                "Linked to Putin associates")
        },
            ["maduro"] = new()
        {
            new("Unnamed Venezuelan Entity", "Venezuela", "Offshore Entity", "Paradise Papers",
                "Linked to Venezuelan officials")
        },
            ["offshore"] = new()
        {
            new("Generic Offshore Entity", "British Virgin Islands", "Shell Company", "Paradise Papers",
                "Tax haven entity")
        },
            ["appleby"] = new()
        {
            new("Appleby", "Bermuda", "Law Firm", "Paradise Papers",
                "Offshore legal services provider")
        }
        };

        public override ScreeningSource Source => ScreeningSource.OffshoreLeaks;

        public OffshoreLeaksScrapingService(
            ILogger<OffshoreLeaksScrapingService> logger,
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
                Logger.LogInformation("Searching Offshore Leaks database (Mock Data) for entity: {EntityName}", entityName.Value);

                await Task.Delay(Random.Shared.Next(300, 900), cancellationToken);

                var hits = new List<ScreeningHit>();
                var searchTerm = entityName.Value.ToLower();

                foreach (var (keyword, leaks) in OffshoreDatabase)
                {
                    if (searchTerm.Contains(keyword))
                    {
                        foreach (var leak in leaks)
                        {
                            hits.Add(ScreeningHit.CreateOffshoreLeaksHit(
                                entityName: leak.EntityName,
                                jurisdiction: leak.Jurisdiction,
                                linkedTo: leak.LinkedTo,
                                dataFrom: leak.DataSource
                            ));
                        }
                    }
                }

                if (hits.Count == 0 && searchTerm.Length > 3)
                {
                    foreach (var (keyword, leaks) in OffshoreDatabase)
                    {
                        foreach (var leak in leaks)
                        {
                            if (leak.EntityName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            {
                                hits.Add(ScreeningHit.CreateOffshoreLeaksHit(
                                    entityName: leak.EntityName,
                                    jurisdiction: leak.Jurisdiction,
                                    linkedTo: leak.LinkedTo,
                                    dataFrom: leak.DataSource
                                ));
                            }
                        }
                    }
                }

                Logger.LogInformation("Found {Count} hits in Offshore Leaks for {EntityName}", hits.Count, entityName.Value);
                if (hits.Count > 0)
                {
                    Logger.LogWarning("NOTE: Using mock Offshore Leaks data based on real ICIJ cases for demonstration");
                }

                return hits;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error searching Offshore Leaks for entity: {EntityName}", entityName.Value);
                throw new ScrapingException(Source, ex.Message, ex);
            }
        }

        private record OffshoreLeakData(
            string EntityName,
            string Jurisdiction,
            string Type,
            string DataSource,
            string LinkedTo);
    }
}