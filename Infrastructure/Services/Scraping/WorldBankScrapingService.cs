using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using System.Text.Json;
using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.Exceptions;
using WebScraping.Domain.ValueObjects;
using WebScraping.Infrastructure.Configuration;

namespace WebScraping.Infrastructure.Services.Scraping
{
    public class WorldBankScrapingService : BaseScrapingService
    {
        public override ScreeningSource Source => ScreeningSource.WorldBank;

        public WorldBankScrapingService(
            ILogger<WorldBankScrapingService> logger,
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
                Logger.LogInformation("Scraping World Bank via internal API for entity: {EntityName}", entityName.Value);

                var hits = new List<ScreeningHit>();

                
                var apiUrl = "https://apigwext.worldbank.org/dvsvc/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM";

                var response = await RetryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(Settings.TimeoutSeconds));

                    
                    var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                    
                    request.Headers.Add("apikey", "z9duUaFUiEUYSHs97CU38fcZO7ipOPvm");
                    request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                    request.Headers.Add("Origin", "https://projects.worldbank.org");
                    request.Headers.Add("Referer", "https://projects.worldbank.org/");

                    var httpResponse = await HttpClient.SendAsync(request, cts.Token);
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsStringAsync(cts.Token);
                });

                Logger.LogDebug("API response received, parsing JSON...");

                using var doc = JsonDocument.Parse(response);

                var searchTerm = entityName.Value.ToLower();

                if (doc.RootElement.TryGetProperty("response", out var responseObj) &&
                    responseObj.TryGetProperty("ZPROCSUPP", out var firmsArray) &&
                    firmsArray.ValueKind == JsonValueKind.Array)
                
                    {
                        foreach (var item in firmsArray.EnumerateArray())
                        {
                            
                            var firmName = GetJsonString(item, "SUPP_NAME");

                            if (string.IsNullOrWhiteSpace(firmName))
                                continue;

                            
                            if (!firmName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                continue;

                            
                            var additionalInfo = GetJsonString(item, "ADD_SUPP_INFO");
                            var city = GetJsonString(item, "SUPP_CITY");
                            var address = GetJsonString(item, "SUPP_ADDR");
                            var country = GetJsonString(item, "COUNTRY_NAME");
                            var fromDate = GetJsonString(item, "DEBAR_FROM_DATE");
                            var toDate = GetJsonString(item, "DEBAR_TO_DATE");
                            var grounds = GetJsonString(item, "DEBAR_REASON");

                            
                            var fullAddress = string.IsNullOrWhiteSpace(address) && string.IsNullOrWhiteSpace(city)
                                ? "N/A"
                                : $"{address}, {city}".Trim(' ', ',');

                            
                            var fullFirmName = string.IsNullOrWhiteSpace(additionalInfo)
                                ? firmName
                                : $"{firmName} ({additionalInfo})";

                            var hit = ScreeningHit.CreateWorldBankHit(
                                firmName: fullFirmName,
                                address: fullAddress,
                                country: country ?? "N/A",
                                fromDate: fromDate ?? "N/A",
                                toDate: toDate ?? "N/A",
                                grounds: grounds ?? "N/A"
                            );

                            hits.Add(hit);

                            Logger.LogDebug("Match found: {FirmName} - {Country}", fullFirmName, country);
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Unexpected JSON structure. Expected: response.ZPROCSUPP array");
                    }

                    Logger.LogInformation("Found {Count} hits in World Bank (via API) for {EntityName}", hits.Count, entityName.Value);

                    return hits;
                }
        catch (Exception ex)
            {
                Logger.LogError(ex, "Error calling World Bank API for entity: {EntityName}", entityName.Value);

                
                Logger.LogWarning("Returning empty results due to API error");
                return new List<ScreeningHit>();
            }
        }

        private string? GetJsonString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();

                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt64().ToString();

                if (prop.ValueKind == JsonValueKind.Null)
                    return null;
            }
            return null;
        }
    }
}